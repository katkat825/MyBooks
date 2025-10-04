namespace MyBooks.Common.Dtos;

public class FileValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}