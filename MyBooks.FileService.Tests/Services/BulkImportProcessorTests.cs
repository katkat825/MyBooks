using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;
using MyBooks.FileService.Tests.Infrastructure;
using Xunit;

namespace MyBooks.FileService.Tests.Services;

/// <summary>
/// Every collaborator below the status machine reaches out to Google, so these tests
/// cover the parts that can be exercised without a network: the entry gate, the missing
/// integration path and the terminal transitions.
/// </summary>
public class BulkImportProcessorTests
{
    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceUrls:CatalogService"] = "http://catalog",
            ["ServiceSecrets:FileService"] = "file-secret",
            ["GoogleOAuth:ClientId"] = "client-id",
            ["GoogleOAuth:ClientSecret"] = "client-secret"
        }).Build();

    private static BulkImportProcessor Build(FileDbContext context)
    {
        var config = Config();
        var http = new HttpClient { BaseAddress = new Uri("http://catalog") };

        return new BulkImportProcessor(
            context,
            new GoogleDriveClient(config, new HttpClient()),
            new SystemTokenHelper(new HttpClient { BaseAddress = new Uri("http://auth") }, "http://auth"),
            http,
            config,
            new HtmlSanitizationService(),
            new FileValidationService());
    }

    private static FileScanDto Scan() => new()
    {
        BulkImportStart = new BulkImportStartDto
        {
            FileIds = new List<string>(),
            GenreId = 1,
            AgeCategoryId = 3,
            IntegrationId = 1,
            PickerAccessToken = "picker-token"
        },
        UserId = "42",
        IpAddress = "203.0.113.10"
    };

    private static BulkImportJob NewJob(string status, params string[] fileIds)
    {
        var job = new BulkImportJob
        {
            TenantId = 7,
            GoogleIntegrationId = 1,
            Status = status,
            TotalFiles = fileIds.Length,
            ProcessedFiles = 0,
            CreatedBy = "42",
            CreatedDate = DateTime.UtcNow
        };

        foreach (var id in fileIds)
        {
            job.Items.Add(new BulkImportItem
            {
                TenantId = 7,
                FileId = id,
                Status = "Pending",
                GenreId = 1,
                AgeCategoryId = 3,
                CreatedBy = "42",
                CreatedDate = DateTime.UtcNow
            });
        }

        return job;
    }

    private static GoogleIntegration NewIntegration() => new()
    {
        Id = 1,
        TenantId = 7,
        AccountEmail = "ada@example.com",
        RefreshToken = "refresh-token",
        IsActive = true,
        CreatedBy = "42",
        CreatedDate = DateTime.UtcNow
    };

    [Fact]
    public async Task Does_nothing_for_an_unknown_job()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        await Build(context).ProcessJobAsync(404, Scan());

        Assert.Empty(await context.BulkImportJobs.ToListAsync());
    }

    [Theory]
    [InlineData("Running")]
    [InlineData("Completed")]
    [InlineData("Failed")]
    [InlineData("CompletedWithErrors")]
    public async Task Refuses_to_reprocess_a_job_that_is_not_pending(string status)
    {
        // Guards against a double-dispatch re-importing books that already landed.
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob(status, "drive-file-1");
        seed.BulkImportJobs.Add(job);
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        var reloaded = await act.BulkImportJobs.SingleAsync();
        Assert.Equal(status, reloaded.Status);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("RetryFails")]
    public async Task Accepts_a_job_in_a_runnable_state(string status)
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob(status);
        seed.BulkImportJobs.Add(job);
        seed.GoogleIntegrations.Add(NewIntegration());
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        var reloaded = await act.BulkImportJobs.SingleAsync();
        Assert.NotEqual(status, reloaded.Status);
    }

    [Fact]
    public async Task Fails_the_job_when_the_integration_is_gone()
    {
        // The user disconnected Drive between queueing and processing.
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob("Pending", "drive-file-1", "drive-file-2");
        seed.BulkImportJobs.Add(job);
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        var reloaded = await act.BulkImportJobs.Include(j => j.Items).SingleAsync();
        Assert.Equal("Failed", reloaded.Status);
        Assert.Equal("Google integration not found", reloaded.ErrorMessage);
        Assert.All(reloaded.Items, item =>
        {
            Assert.Equal("Failed", item.Status);
            Assert.Equal("Google integration not found", item.ErrorMessage);
        });
    }

    [Fact]
    public async Task Fails_the_job_when_the_integration_is_deactivated()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob("Pending", "drive-file-1");
        var integration = NewIntegration();
        integration.IsActive = false;
        seed.BulkImportJobs.Add(job);
        seed.GoogleIntegrations.Add(integration);
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        Assert.Equal("Failed", (await act.BulkImportJobs.SingleAsync()).Status);
    }

    [Fact]
    public async Task Will_not_use_another_tenants_integration()
    {
        // The lookup runs with IgnoreQueryFilters, so the explicit TenantId match in the
        // predicate is the only thing keeping tenants apart here.
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob("Pending", "drive-file-1");
        var foreign = NewIntegration();
        foreign.TenantId = 99;
        seed.BulkImportJobs.Add(job);
        seed.GoogleIntegrations.Add(foreign);
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        Assert.Equal("Failed", (await act.BulkImportJobs.SingleAsync()).Status);
    }

    [Fact]
    public async Task Completes_a_job_with_no_items()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        var job = NewJob("Pending");
        seed.BulkImportJobs.Add(job);
        seed.GoogleIntegrations.Add(NewIntegration());
        await seed.SaveChangesAsSystemAsync();

        await Build(act).ProcessJobAsync(job.Id, Scan());

        var reloaded = await act.BulkImportJobs.SingleAsync();
        Assert.Equal("Completed", reloaded.Status);
        Assert.NotNull(reloaded.LastModifiedDate);
    }

    [Fact]
    public void New_jobs_start_pending()
    {
        Assert.Equal("Pending", new BulkImportJob().Status);
        Assert.Equal("Pending", new BulkImportItem().Status);
    }
}
