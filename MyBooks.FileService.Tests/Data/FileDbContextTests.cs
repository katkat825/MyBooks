using System.Security;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Models;
using MyBooks.FileService.Tests.Infrastructure;
using Xunit;

namespace MyBooks.FileService.Tests.Data;

/// <summary>
/// EnforceSecurityRules is what stops a file row being written without provenance, and
/// the query filters are what stop one tenant reading another's library.
/// </summary>
public class FileDbContextTests
{
    private static FileMetadata NewFile(int tenantId = 7, string name = "book.pdf") => new()
    {
        TenantId = tenantId,
        FileName = name,
        FilePath = "drive-file-id",
        ContentType = "application/pdf",
        FileSize = 1024,
        BookId = 1,
        UploadedByIp = "203.0.113.10",
        CreatedBy = "42",
        CreatedDate = DateTime.UtcNow
    };

    [Fact]
    public async Task Files_from_another_tenant_are_invisible()
    {
        var accessor = TestContext.Accessor("42", tenantId: 7);
        var (seed, act) = TestContext.DbPair(accessor);

        seed.Files.AddRange(NewFile(7, "mine.pdf"), NewFile(99, "theirs.pdf"));
        await seed.SaveChangesAsSystemAsync();

        var visible = await act.Files.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("mine.pdf", visible[0].FileName);
    }

    [Fact]
    public async Task Inactive_files_are_invisible()
    {
        var accessor = TestContext.Accessor("42", tenantId: 7);
        var (seed, act) = TestContext.DbPair(accessor);

        var archived = NewFile(7, "archived.pdf");
        archived.IsActive = false;
        seed.Files.AddRange(NewFile(7, "live.pdf"), archived);
        await seed.SaveChangesAsSystemAsync();

        Assert.Single(await act.Files.ToListAsync());
    }

    [Fact]
    public async Task Integrations_from_another_tenant_are_invisible()
    {
        var accessor = TestContext.Accessor("42", tenantId: 7);
        var (seed, act) = TestContext.DbPair(accessor);

        seed.GoogleIntegrations.AddRange(
            new GoogleIntegration
            {
                TenantId = 7, AccountEmail = "mine@example.com", RefreshToken = "a",
                CreatedBy = "42", CreatedDate = DateTime.UtcNow
            },
            new GoogleIntegration
            {
                TenantId = 99, AccountEmail = "theirs@example.com", RefreshToken = "b",
                CreatedBy = "42", CreatedDate = DateTime.UtcNow
            });
        await seed.SaveChangesAsSystemAsync();

        var visible = await act.GoogleIntegrations.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("mine@example.com", visible[0].AccountEmail);
    }

    [Fact]
    public async Task A_new_file_row_requires_a_valid_ip_address()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var file = NewFile();
        file.UploadedByIp = "not-an-ip";
        context.Files.Add(file);

        var ex = await Assert.ThrowsAsync<SecurityException>(
            () => context.SaveChangesAsSystemAsync("42", "not-an-ip"));
        Assert.Equal("A valid IP address is required.", ex.Message);
    }

    [Fact]
    public async Task A_new_file_row_requires_a_numeric_user_id()
    {
        using var context = TestContext.Db(TestContext.NoContext());
        context.Files.Add(NewFile());

        var ex = await Assert.ThrowsAsync<SecurityException>(
            () => context.SaveChangesAsSystemAsync("anonymous", "203.0.113.10"));
        Assert.Equal("A valid user ID is required", ex.Message);
    }

    [Fact]
    public async Task A_new_file_row_records_who_uploaded_it_and_from_where()
    {
        using var context = TestContext.Db(TestContext.NoContext());
        var file = NewFile();
        context.Files.Add(file);

        await context.SaveChangesAsSystemAsync("42", "203.0.113.10");

        Assert.Equal("42", file.CreatedBy);
        Assert.Equal("203.0.113.10", file.UploadedByIp);
    }

    [Fact]
    public async Task Deleting_a_file_archives_it_rather_than_removing_it()
    {
        // Files are referenced by the catalog and by reading progress; a hard delete
        // would orphan both.
        var accessor = TestContext.Accessor("42", tenantId: 7);
        var (seed, act) = TestContext.DbPair(accessor);

        seed.Files.Add(NewFile());
        await seed.SaveChangesAsSystemAsync();

        var tracked = await act.Files.SingleAsync();
        act.Files.Remove(tracked);
        await act.SaveChangesAsync();

        var survivor = await act.Files.IgnoreQueryFilters().SingleAsync();
        Assert.False(survivor.IsActive);
    }

    [Fact]
    public async Task System_save_refuses_an_entity_with_no_created_date()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var job = new BulkImportJob
        {
            TenantId = 7,
            GoogleIntegrationId = 1,
            CreatedBy = "42",
            CreatedDate = default
        };
        context.BulkImportJobs.Add(job);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsSystemAsync());
        Assert.Equal("System save requires CreatedBy and CreatedDate to be set.", ex.Message);
    }

    [Fact]
    public void Ip_address_falls_back_to_unknown_without_a_request()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        Assert.Equal("unknown", context.GetCurrentUserIpAddress());
    }

    [Fact]
    public void Ip_address_is_read_from_the_forwarded_header()
    {
        // The services sit behind a reverse proxy, so the socket address is the proxy.
        using var context = TestContext.Db(
            TestContext.Accessor("42", 7, ip: "198.51.100.7, 203.0.113.1"));

        Assert.Equal("198.51.100.7", context.GetCurrentUserIpAddress());
    }

    [Fact]
    public void Role_falls_back_to_empty_without_a_claim()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        Assert.Equal(string.Empty, context.GetCurrentUserRole());
    }

    [Fact]
    public async Task Reading_progress_is_not_tenant_filtered()
    {
        // Documents a real gap rather than asserting intent: ReadingProgress carries a
        // TenantId but has no query filter, so isolation depends entirely on callers
        // filtering by user id.
        var accessor = TestContext.Accessor("42", tenantId: 7);
        var (seed, act) = TestContext.DbPair(accessor);

        seed.ReadingProgresses.AddRange(
            new ReadingProgress { TenantId = 7, FileId = 1, UserId = 42, ProgressPercent = 10, LastUpdated = DateTime.UtcNow },
            new ReadingProgress { TenantId = 99, FileId = 2, UserId = 77, ProgressPercent = 20, LastUpdated = DateTime.UtcNow });
        await seed.SaveChangesAsync();

        Assert.Equal(2, await act.ReadingProgresses.CountAsync());
    }
}
