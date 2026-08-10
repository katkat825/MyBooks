using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Tests.Infrastructure;
using Xunit;

namespace MyBooks.FileService.Tests.Controllers;

public class ReadingProgressControllerTests
{
    private static ReadingProgressController Build(FileDbContext context, string? userId = "42")
    {
        var http = new DefaultHttpContext
        {
            User = userId is null
                ? new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())
                : TestContext.Principal(userId, 7)
        };

        return new ReadingProgressController(context)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static ReadingProgress Progress(int fileId, int userId, double percent, DateTime when)
        => new()
        {
            TenantId = 7,
            FileId = fileId,
            UserId = userId,
            ProgressPercent = percent,
            LastUpdated = when
        };

    [Fact]
    public async Task Recent_progress_requires_a_user_id()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var result = await Build(context).GetRecentProgress(0);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A valid user ID is required", bad.Value);
    }

    [Fact]
    public async Task Recent_progress_excludes_finished_books()
    {
        // The "Continue Reading" shelf should not resurface a book the user just finished.
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        seed.ReadingProgresses.AddRange(
            Progress(1, 42, 50, DateTime.UtcNow),
            Progress(2, 42, 99, DateTime.UtcNow),
            Progress(3, 42, 100, DateTime.UtcNow));
        await seed.SaveChangesAsync();

        var ok = Assert.IsType<OkObjectResult>(await Build(act).GetRecentProgress(42));
        var items = Assert.IsAssignableFrom<IEnumerable<MyBooks.Common.Dtos.ReadingProgressDto>>(ok.Value);

        Assert.Single(items);
        Assert.Equal(1, items.Single().FileId);
    }

    [Fact]
    public async Task Recent_progress_is_newest_first()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        seed.ReadingProgresses.AddRange(
            Progress(1, 42, 10, DateTime.UtcNow.AddDays(-3)),
            Progress(2, 42, 20, DateTime.UtcNow.AddDays(-1)),
            Progress(3, 42, 30, DateTime.UtcNow.AddDays(-2)));
        await seed.SaveChangesAsync();

        var ok = (OkObjectResult)await Build(act).GetRecentProgress(42);
        var items = ((IEnumerable<MyBooks.Common.Dtos.ReadingProgressDto>)ok.Value!).ToList();

        Assert.Equal(new[] { 2, 3, 1 }, items.Select(i => i.FileId));
    }

    [Fact]
    public async Task Recent_progress_respects_the_requested_count()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        for (var i = 1; i <= 5; i++)
            seed.ReadingProgresses.Add(Progress(i, 42, 10, DateTime.UtcNow.AddMinutes(-i)));
        await seed.SaveChangesAsync();

        var ok = (OkObjectResult)await Build(act).GetRecentProgress(42, count: 2);

        Assert.Equal(2, ((IEnumerable<MyBooks.Common.Dtos.ReadingProgressDto>)ok.Value!).Count());
    }

    [Fact]
    public async Task Recent_progress_is_scoped_to_the_requested_user()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        seed.ReadingProgresses.AddRange(
            Progress(1, 42, 10, DateTime.UtcNow),
            Progress(2, 77, 10, DateTime.UtcNow));
        await seed.SaveChangesAsync();

        var ok = (OkObjectResult)await Build(act).GetRecentProgress(42);
        var items = ((IEnumerable<MyBooks.Common.Dtos.ReadingProgressDto>)ok.Value!).ToList();

        Assert.Single(items);
        Assert.Equal(1, items[0].FileId);
    }

    [Fact]
    public async Task Reading_a_position_requires_authentication()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var result = await Build(context, userId: null).GetReadingProgress(1);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not identified", unauthorized.Value);
    }

    [Fact]
    public async Task An_unread_book_reports_zero_progress()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var ok = Assert.IsType<OkObjectResult>(await Build(context).GetReadingProgress(1));

        var percent = ok.Value!.GetType().GetProperty("ProgressPercent")!.GetValue(ok.Value);
        Assert.Equal(0, Convert.ToDouble(percent));
    }

    [Fact]
    public async Task Saving_progress_requires_authentication()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var result = await Build(context, userId: null)
            .UpdateReadingProgress(1, new ReadingProgressUpdateDto { ProgressPercent = 50 });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User not identified.", unauthorized.Value);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    [InlineData(1000)]
    public async Task Rejects_progress_outside_zero_to_one_hundred(double percent)
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var result = await Build(context)
            .UpdateReadingProgress(1, new ReadingProgressUpdateDto { ProgressPercent = percent });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ProgressPercent must be between 0 and 100.", bad.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task Accepts_the_boundary_values(double percent)
    {
        using var context = TestContext.Db(TestContext.NoContext());

        var result = await Build(context)
            .UpdateReadingProgress(1, new ReadingProgressUpdateDto { ProgressPercent = percent });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Saving_progress_for_the_first_time_creates_a_row()
    {
        using var context = TestContext.Db(TestContext.NoContext());

        await Build(context).UpdateReadingProgress(
            5, new ReadingProgressUpdateDto { ProgressPercent = 42 });

        var stored = await context.ReadingProgresses.SingleAsync();
        Assert.Equal(5, stored.FileId);
        Assert.Equal(42, stored.UserId);
        Assert.Equal(42, stored.ProgressPercent);
    }

    [Fact]
    public async Task Saving_progress_again_updates_the_same_row()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        seed.ReadingProgresses.Add(Progress(5, 42, 10, DateTime.UtcNow.AddDays(-1)));
        await seed.SaveChangesAsync();

        await Build(act).UpdateReadingProgress(
            5, new ReadingProgressUpdateDto { ProgressPercent = 75 });

        var stored = await act.ReadingProgresses.SingleAsync();
        Assert.Equal(75, stored.ProgressPercent);
        Assert.InRange(stored.LastUpdated, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task One_readers_progress_does_not_overwrite_anothers()
    {
        var (seed, act) = TestContext.DbPair(TestContext.NoContext());
        seed.ReadingProgresses.Add(Progress(5, 77, 90, DateTime.UtcNow));
        await seed.SaveChangesAsync();

        await Build(act).UpdateReadingProgress(
            5, new ReadingProgressUpdateDto { ProgressPercent = 10 });

        var other = await act.ReadingProgresses.SingleAsync(p => p.UserId == 77);
        Assert.Equal(90, other.ProgressPercent);
        Assert.Equal(2, await act.ReadingProgresses.CountAsync());
    }
}
