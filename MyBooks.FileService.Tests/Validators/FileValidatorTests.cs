using Microsoft.AspNetCore.Http;
using MyBooks.FileService.Tests.Infrastructure;
using MyBooks.FileService.Validators;
using NSubstitute;
using Xunit;

namespace MyBooks.FileService.Tests.Validators;

/// <summary>
/// The extension and MIME rules are advisory; the magic-byte rule is the one that
/// actually stops a renamed executable from being accepted as a book.
/// </summary>
public class FileValidatorTests
{
    private readonly FileValidator _validator = new();

    private static IFormFile File(
        byte[] content,
        string fileName = "book.pdf",
        string contentType = "application/pdf",
        long? length = null)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);
        file.Length.Returns(length ?? content.Length);
        file.OpenReadStream().Returns(_ => new MemoryStream(content));
        return file;
    }

    [Fact]
    public void Accepts_a_real_pdf()
    {
        Assert.True(_validator.Validate(File(SampleFiles.MinimalPdf())).IsValid);
    }

    [Fact]
    public void Accepts_a_real_epub()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalEpub(), "book.epub", "application/epub+zip"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_an_empty_file()
    {
        var result = _validator.Validate(File(Array.Empty<byte>(), length: 0));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "File must not be empty.");
    }

    [Fact]
    public void Rejects_a_file_over_one_gigabyte()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalPdf(), length: 1073741825));

        Assert.Contains(result.Errors, e => e.ErrorMessage == "File size must be below 1GB.");
    }

    [Fact]
    public void Accepts_a_file_at_exactly_one_gigabyte()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalPdf(), length: 1073741824));

        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage == "File size must be below 1GB.");
    }

    [Theory]
    [InlineData("book.txt")]
    [InlineData("book.exe")]
    [InlineData("book")]
    [InlineData("book.pdf.exe")]
    public void Rejects_disallowed_extensions(string fileName)
    {
        var result = _validator.Validate(File(SampleFiles.MinimalPdf(), fileName));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "Invalid file extension. Allowed extensions are: .pdf and .epub");
    }

    [Theory]
    [InlineData("BOOK.PDF")]
    [InlineData("Book.Epub")]
    public void Extension_check_is_case_insensitive(string fileName)
    {
        var content = fileName.EndsWith("PDF", StringComparison.OrdinalIgnoreCase)
            ? SampleFiles.MinimalPdf()
            : SampleFiles.MinimalEpub();

        var result = _validator.Validate(File(content, fileName,
            fileName.EndsWith("PDF", StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/epub+zip"));

        Assert.DoesNotContain(result.Errors, e =>
            e.ErrorMessage!.StartsWith("Invalid file extension", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_a_file_name_over_255_characters()
    {
        var name = new string('a', 252) + ".pdf";

        var result = _validator.Validate(File(SampleFiles.MinimalPdf(), name));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "File name must not exceed 255 characters.");
    }

    [Fact]
    public void Rejects_a_disallowed_mime_type()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalPdf(), "book.pdf", "text/plain"));

        Assert.Contains(result.Errors, e => e.ErrorMessage ==
            "Invalid MIME type. Allowed types are: application/pdf, application/epub+zip, text/plain.");
    }

    [Fact]
    public void Rejects_an_executable_renamed_to_pdf()
    {
        // The whole point of the signature rule. MZ is a Windows executable header.
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04 };

        var result = _validator.Validate(File(exe, "totally-a-book.pdf"));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "File signature does not match the expected format.");
    }

    [Fact]
    public void Rejects_a_zip_renamed_to_pdf()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalEpub(), "not-really.pdf"));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "File signature does not match the expected format.");
    }

    [Fact]
    public void Rejects_a_pdf_renamed_to_epub()
    {
        var result = _validator.Validate(
            File(SampleFiles.MinimalPdf(), "not-really.epub", "application/epub+zip"));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "File signature does not match the expected format.");
    }

    [Fact]
    public void Rejects_a_file_shorter_than_the_signature_probe()
    {
        // Fewer than eight bytes cannot be inspected, so the file is refused rather than
        // waved through.
        var result = _validator.Validate(File(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }));

        Assert.Contains(result.Errors, e =>
            e.ErrorMessage == "File signature does not match the expected format.");
    }
}
