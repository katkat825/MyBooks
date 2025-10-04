using MyBooks.Common.Dtos;
using System.IO.Compression;
using System.Xml.Linq;

namespace MyBooks.FileService.Services;

public class FileValidationService
{
    public async Task<FileValidationResultDto> ValidateAsync(Stream fileStream, string fileName)
    {
        try
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var pdf = UglyToad.PdfPig.PdfDocument.Open(fileStream, new UglyToad.PdfPig.ParsingOptions() { ClipPaths = true });

                    int pageCount = pdf.NumberOfPages;
                    if (pageCount <= 0)
                        return new FileValidationResultDto { IsValid = false, ErrorMessage = "PDF has no pages." };

                    // determine which pages to test (1, middle, last)
                    var pagesToCheck = new List<int> { 1 };
                    if (pageCount > 2)
                        pagesToCheck.Add(pageCount / 2);
                    if (pageCount > 1)
                        pagesToCheck.Add(pageCount);

                    foreach (var pageNumber in pagesToCheck.Distinct())
                    {
                        try
                        {
                            var page = pdf.GetPage(pageNumber);

                            // try to read some basic info
                            var text = page.Text?.Substring(0, Math.Min(page.Text.Length, 50)) ?? string.Empty;

                            // if it doesn't throw, page is readable
                        }
                        catch (Exception ex)
                        {
                            return new FileValidationResultDto
                            {
                                IsValid = false,
                                ErrorMessage = $"PDF page {pageNumber} could not be read: {ex.Message}"
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new FileValidationResultDto
                    {
                        IsValid = false,
                        ErrorMessage = $"PDF validation failed: {ex.Message}"
                    };
                }
            }

            else if (fileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                    var containerEntry = archive.GetEntry("META-INF/container.xml");
                    if (containerEntry == null)
                        return new FileValidationResultDto { IsValid = false, ErrorMessage = "EPUB missing container.xml." };

                    using var containerStream = containerEntry.Open();
                    var containerXml = XDocument.Load(containerStream);

                    XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
                    var rootfileElement = containerXml.Descendants(ns + "rootfile").FirstOrDefault();
                    if (rootfileElement == null)
                        return new FileValidationResultDto { IsValid = false, ErrorMessage = "EPUB missing rootfile reference." };

                    var fullPath = rootfileElement.Attribute("full-path")?.Value;
                    if (string.IsNullOrWhiteSpace(fullPath))
                        return new FileValidationResultDto { IsValid = false, ErrorMessage = "EPUB missing full-path attribute." };

                    if (archive.GetEntry(fullPath) == null)
                        return new FileValidationResultDto { IsValid = false, ErrorMessage = "EPUB missing referenced OPF file." };
                }
                catch (Exception ex)
                {
                    return new FileValidationResultDto { IsValid = false, ErrorMessage = $"EPUB validation failed: {ex.Message}" };
                }
            }
            else
            {
                return new FileValidationResultDto { IsValid = false, ErrorMessage = "Unsupported file type." };
            }

            return new FileValidationResultDto { IsValid = true };
        }
        catch (Exception ex)
        {
            return new FileValidationResultDto { IsValid = false, ErrorMessage = ex.Message };
        }
        finally
        {
            fileStream.Seek(0, SeekOrigin.Begin);
        }
    }
}