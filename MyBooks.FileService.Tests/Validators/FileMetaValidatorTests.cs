using MyBooks.FileService.Models;
using MyBooks.FileService.Validators;
using Xunit;

namespace MyBooks.FileService.Tests.Validators;

public class FileMetaValidatorTests
{
    private readonly FileMetaValidator _validator = new();

    private static FileMetadata Valid() => new()
    {
        FileName = "book.pdf",
        BookId = 1,
        FilePath = "drive-file-id",
        UploadedByIp = "203.0.113.10"
    };

    [Fact]
    public void Accepts_valid_metadata()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Requires_a_file_name()
    {
        var meta = Valid();
        meta.FileName = string.Empty;

        Assert.Contains(_validator.Validate(meta).Errors, e =>
            e.PropertyName == "FileName" && e.ErrorMessage == "File name is required.");
    }

    [Fact]
    public void Rejects_a_file_name_over_255_characters()
    {
        var meta = Valid();
        meta.FileName = new string('a', 256);

        Assert.Contains(_validator.Validate(meta).Errors, e =>
            e.ErrorMessage == "File name must not exceed 255 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Requires_the_metadata_to_point_at_a_book(int bookId)
    {
        // An orphaned file row is unreachable through the catalog and never gets cleaned up.
        var meta = Valid();
        meta.BookId = bookId;

        Assert.Contains(_validator.Validate(meta).Errors, e =>
            e.PropertyName == "BookId" && e.ErrorMessage == "Book ID is required.");
    }
}
