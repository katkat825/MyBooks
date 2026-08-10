using Microsoft.Extensions.Configuration;
using MyBooks.FileService.Services;
using MyBooks.FileService.Tests.Infrastructure;
using Xunit;

namespace MyBooks.FileService.Tests.Services;

public class ClamAvScanServiceTests
{
    private static ClamAvScanService Build(string host = "127.0.0.1", string port = "1")
        => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CLAMAV_HOST"] = host,
            ["ClamAV:Port"] = port
        }).Build());

    [Fact]
    public async Task Fails_open_when_the_scanner_is_unreachable()
    {
        // Deliberate product decision, captured here so it cannot change silently: if
        // ClamAV is down, uploads continue rather than the app becoming unusable. Anyone
        // tightening this to fail-closed should have to delete this test on purpose.
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        Assert.True(await Build().IsFileCleanAsync(stream));
    }

    [Fact]
    public async Task Rewinds_the_stream_after_scanning()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        await Build().IsFileCleanAsync(stream);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Rewinds_the_stream_even_when_the_scanner_is_unreachable()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());
        _ = await Build(port: "1").IsFileCleanAsync(stream);

        Assert.Equal(0, stream.Position);
    }
}
