using MyBooks.FileService.Services;
using MyBooks.FileService.Tests.Infrastructure;
using Xunit;

namespace MyBooks.FileService.Tests.Services;

/// <summary>
/// This is the corruption gate for bulk import. A file that passes here is committed to
/// the catalog, so a false positive means a broken book in a user's library.
/// </summary>
public class FileValidationServiceTests
{
    private readonly FileValidationService _service = new();

    [Fact]
    public async Task Accepts_a_well_formed_pdf()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        var result = await _service.ValidateAsync(stream, "book.pdf");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_a_corrupt_pdf()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.Corrupt());

        var result = await _service.ValidateAsync(stream, "book.pdf");

        Assert.False(result.IsValid);
        Assert.StartsWith("PDF validation failed:", result.ErrorMessage);
    }

    [Fact]
    public async Task Accepts_a_well_formed_epub()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalEpub());

        var result = await _service.ValidateAsync(stream, "book.epub");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_an_epub_with_no_container()
    {
        await using var stream = SampleFiles.AsStream(
            SampleFiles.MinimalEpub(includeContainer: false));

        var result = await _service.ValidateAsync(stream, "book.epub");

        Assert.False(result.IsValid);
        Assert.Equal("EPUB missing container.xml.", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_an_epub_with_no_rootfile_reference()
    {
        await using var stream = SampleFiles.AsStream(
            SampleFiles.MinimalEpub(includeRootfile: false));

        var result = await _service.ValidateAsync(stream, "book.epub");

        Assert.False(result.IsValid);
        Assert.Equal("EPUB missing rootfile reference.", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_an_epub_whose_opf_is_missing()
    {
        // The container points at a file that is not in the archive, which is exactly what
        // a truncated or partially uploaded epub looks like.
        await using var stream = SampleFiles.AsStream(
            SampleFiles.MinimalEpub(includeOpf: false));

        var result = await _service.ValidateAsync(stream, "book.epub");

        Assert.False(result.IsValid);
        Assert.Equal("EPUB missing referenced OPF file.", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_an_epub_that_is_not_a_zip()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.Corrupt());

        var result = await _service.ValidateAsync(stream, "book.epub");

        Assert.False(result.IsValid);
        Assert.StartsWith("EPUB validation failed:", result.ErrorMessage);
    }

    [Theory]
    [InlineData("book.txt")]
    [InlineData("book.mobi")]
    [InlineData("book")]
    public async Task Rejects_unsupported_extensions(string fileName)
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        var result = await _service.ValidateAsync(stream, fileName);

        Assert.False(result.IsValid);
        Assert.Equal("Unsupported file type.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("BOOK.PDF")]
    [InlineData("Book.pDf")]
    public async Task Extension_matching_is_case_insensitive(string fileName)
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        Assert.True((await _service.ValidateAsync(stream, fileName)).IsValid);
    }

    [Fact]
    public async Task Never_throws_on_hostile_input()
    {
        // Bulk import calls this in a loop; an exception here would abort the whole job
        // rather than failing a single item.
        await using var stream = SampleFiles.AsStream(new byte[] { 0x00 });

        var result = await _service.ValidateAsync(stream, "book.pdf");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task Rewinds_the_stream_so_the_caller_can_reuse_it()
    {
        // The import pipeline validates and then immediately reads the same stream to
        // extract metadata.
        await using var stream = SampleFiles.AsStream(SampleFiles.MinimalPdf());

        await _service.ValidateAsync(stream, "book.pdf");

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Rewinds_the_stream_even_after_a_failure()
    {
        await using var stream = SampleFiles.AsStream(SampleFiles.Corrupt());

        await _service.ValidateAsync(stream, "book.pdf");

        Assert.Equal(0, stream.Position);
    }
}
