using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using System.IO.Compression;
using System.Xml.Linq;

namespace MyBooks.FileService.Services;

public class BulkImportProcessor
{
    private readonly FileDbContext _context;
    private readonly GoogleDriveClient _googleDriveClient;
    private readonly SystemTokenHelper _tokenHelper;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly HtmlSanitizationService _sanitizer;
    private readonly FileValidationService _antiCorrpution;

    public BulkImportProcessor(
        FileDbContext context,
        GoogleDriveClient googleDriveClient,
        SystemTokenHelper tokenHelper,
        HttpClient httpClient,
        IConfiguration config,
        HtmlSanitizationService sanitizer,
        FileValidationService antiCorruption)
    {
        _context = context;
        _googleDriveClient = googleDriveClient;
        _tokenHelper = tokenHelper;
        _httpClient = httpClient;
        _config = config;
        _sanitizer = sanitizer;
        _antiCorrpution = antiCorruption;
    }

    public async Task ProcessJobAsync(int jobId, FileScanDto scanDto)
    {
        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null || (job.Status != "Pending" && job.Status != "RetryFails"))
        {
            return;
        }

        job.Status = "Running";
        await _context.SaveChangesAsSystemAsync();

        var integration = await _context.GoogleIntegrations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.TenantId == job.TenantId &&
                                    g.IsActive &&
                                    g.Id == job.GoogleIntegrationId);

        if (integration == null)
        {
            foreach (var item in job.Items.Where(i => i.Status == "Pending"))
            {
                item.Status = "Failed";
                item.ErrorMessage = "Google integration not found";
            }

            job.Status = "Failed";
            job.ErrorMessage = "Google integration not found";
            job.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsSystemAsync();
            return;
        }

        var accessToken = await _googleDriveClient.RefreshAccessTokenAsync(integration.RefreshToken);

        // process items - first run
        await ProcessItemsAsync(job, integration, scanDto, accessToken);

        if (job.Items.All(i => i.Status == "Success"))
        {
            job.Status = "Completed";
        }

        else if (job.Items.All(i => i.Status == "Failed"))
        {
            job.Status = "Failed";
        }

        else
        {
            job.Status = "RetryFails";

            await _context.SaveChangesAsSystemAsync();

            foreach (var item in job.Items.Where(i => i.Status == "Failed"))
            {
                item.Status = "Pending";
                item.ErrorMessage = null;
                if(job.ProcessedFiles > 0)
                    job.ProcessedFiles--; // remove failed items from processed count. they'll be processed again
            }

            await _context.SaveChangesAsSystemAsync();

            // retry fails - second run
            await ProcessItemsAsync(job, integration, scanDto, accessToken);

            if (job.Items.All(i => i.Status == "Success"))
                job.Status = "Completed";
            else
                job.Status = "CompletedWithErrors";
        }

        job.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsSystemAsync();
    }

    private async Task ProcessItemsAsync(BulkImportJob job, GoogleIntegration integration, FileScanDto scanDto, string accessToken)
    {
        foreach (var item in job.Items.Where(i => i.Status == "Pending"))
        {
            try
            {
                var file = await _googleDriveClient.GetFileAsync(item.FileId, accessToken);
                if (file == null)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = "File not found in Google Drive";
                    continue;
                }

                item.FileName = _sanitizer.Sanitize(file.Name);

                using var stream = await _googleDriveClient.GetFileStreamAsync(item.FileId, accessToken);

                var notCorrupted = await _antiCorrpution.ValidateAsync(stream, file.Name);
                if (!notCorrupted.IsValid)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = $"File corruption check failed: {notCorrupted.ErrorMessage}";
                    continue;
                }

                // rewind stream
                stream.Seek(0, SeekOrigin.Begin);

                string title;
                string? author;
                string? series = null;
                string? seriesIndex = null;

                if (file.MimeType == "application/pdf")
                {
                    (title, author) = ExtractPdfMetadata(stream, item.FileName);
                }
                else if (file.MimeType == "application/epub+zip")
                {
                    (title, author, series, seriesIndex) = ExtractEpubMetadata(stream, item.FileName);
                }
                else
                {
                    item.Status = "Failed";
                    item.ErrorMessage = "Unsupported file type.";
                    continue;
                }

                var token = await _tokenHelper.GetSystemTokenAsync(
                    "FileService",
                    _config["ServiceSecrets:FileService"]);

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var requestDto = new BookImportRequestDto
                {
                    Title = title,
                    Author = author,
                    GenreId = item.GenreId,
                    AgeCategoryId = item.AgeCategoryId,
                    FilePath = item.FileId,
                    FileName = item.FileName ?? string.Empty,
                    TenantId = job.TenantId,
                    Series = series ?? string.Empty,
                    SeriesIndex = seriesIndex ?? string.Empty
                };

                var catalogUrl = _config["ServiceUrls:CatalogService"];
                var bookResponse = await _httpClient.PostAsJsonAsync($"{catalogUrl}/api/book-import", requestDto);
                bookResponse.EnsureSuccessStatusCode();

                var responseDto = await bookResponse.Content.ReadFromJsonAsync<BookImportResponseDto>();

                if (responseDto == null)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = "Catalog did not return book response.";
                    continue;
                }

                item.CreatedBookId = responseDto.BookId;

                var fileMeta = new FileMetadata
                {
                    FileName = item.FileName,
                    FilePath = item.FileId,
                    ContentType = file.MimeType,
                    FileSize = file.Size ?? 0,
                    BookId = responseDto.BookId,
                    TenantId = job.TenantId,
                    IsActive = true,
                    GoogleIntegrationId = job.GoogleIntegrationId,
                    FolderId = null,
                    StorageSource = StorageSource.GoogleDrive,
                    IsConverted = false,    
                    ConvertedFilePath = null,
                    CreatedBy = scanDto.UserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Files.Add(fileMeta);
                await _context.SaveChangesAsSystemAsync(scanDto.UserId, scanDto.IpAddress);

                item.CreatedFileId = fileMeta.Id;

                /* looks like crap
                // convert to epub 
                if (file.MimeType == "application/pdf")
                {
                    try
                    {
                        using var pdfStream = await _googleDriveClient.GetFileStreamAsync(item.FileId, accessToken);

                        var converter = new PdfToEpubConverter(new CloudflareR2Client(_config));
                        var convertedPath = await converter.ConvertAndUploadAsync(pdfStream, job.TenantId.ToString(), Path.GetFileNameWithoutExtension(item.FileName));

                        if (!string.IsNullOrEmpty(convertedPath))
                        {
                            fileMeta.ConvertedFilePath = convertedPath;
                            fileMeta.IsConverted = true;
                            Console.WriteLine($"[BulkImportProcessor] Conversion success → EPUB stored at {convertedPath}");

                            _context.Files.Update(fileMeta);
                            await _context.SaveChangesAsSystemAsync(scanDto.UserId, scanDto.IpAddress);
                        }
                        else
                            Console.WriteLine($"[BulkImportProcessor] Conversion failed for {item.FileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BulkImportProcessor] Error during conversion/upload for {item.FileName}: {ex.Message}");
                    }
                }
                */

                var linkDto = new BookFileLinkDto
                {
                    BookId = fileMeta.BookId,
                    FileId = fileMeta.Id
                };

                var linkResponse = await _httpClient.PatchAsJsonAsync($"{catalogUrl}/api/book-import/file", linkDto);
                linkResponse.EnsureSuccessStatusCode();

                item.Status = "Success";
            }
            catch (Exception ex)
            {
                item.Status = "Failed";
                item.ErrorMessage = ex.Message;
            }

            job.ProcessedFiles++;
            await _context.SaveChangesAsSystemAsync();
        }
    }

    private (string Title, string? Author) ExtractPdfMetadata(Stream pdfStream, string fallbackName)
    {
        fallbackName = Path.GetFileNameWithoutExtension(fallbackName);

        try
        {
            // pdfpig requires a seekable stream
            pdfStream.Position = 0;

            using (var document = PdfDocument.Open(pdfStream, new ParsingOptions { UseLenientParsing = true }))
            {
                var info = document.Information;

                // pull title, fallback to filename
                string title = !string.IsNullOrWhiteSpace(info.Title)
                    ? _sanitizer.Sanitize(info.Title)
                    : fallbackName;

                // pull author if available
                string? author = !string.IsNullOrWhiteSpace(info.Author)
                    ? _sanitizer.Sanitize(info.Author)
                    : null;

                // optional: if still no title, try first line of page 1 text
                if (title == fallbackName && document.NumberOfPages > 0)
                {
                    var firstPageText = document.GetPage(1).Text;
                    var firstLine = firstPageText.Split('\n')
                                                .Select(l => l.Trim())
                                                .FirstOrDefault(l => !string.IsNullOrEmpty(l));

                    if (!string.IsNullOrWhiteSpace(firstLine))
                    {
                        var sanitized = _sanitizer.Sanitize(firstLine);
                        title = sanitized.Length <= 100 ? sanitized : fallbackName;
                    }
                }

                return (title, author);
            }
        }
        catch
        {
            return (fallbackName, null);
        }
    }

    private (string Title, string? Author, string? Series, string? SeriesIndex) ExtractEpubMetadata(Stream epubStream, string fallbackName)
    {
        fallbackName = Path.GetFileNameWithoutExtension(fallbackName);
        epubStream.Position = 0;
        using var archive = new ZipArchive(epubStream, ZipArchiveMode.Read, leaveOpen: true);

        var containerEntry = archive.GetEntry("META-INF/container.xml") ?? null;
        if (containerEntry == null)
            return (fallbackName, null, null, null);

        using var containerStream = containerEntry.Open();
        var containerXml = XDocument.Load(containerStream);

        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
        var rootfileElement = containerXml
            .Descendants(ns + "rootfile")
            .FirstOrDefault()
            ?? null;

        if (rootfileElement == null)
            return (fallbackName, null, null, null);

        var fullPath = rootfileElement.Attribute("full-path")?.Value;
        if (string.IsNullOrWhiteSpace(fullPath))
            return (fallbackName, null, null, null);

        var opfEntry = archive.GetEntry(fullPath) ?? null;
        if (opfEntry == null)
            return (fallbackName, null, null, null);

        using var opfStream = opfEntry.Open();
        var opfXml = XDocument.Load(opfStream);

        XNamespace dc = "http://purl.org/dc/elements/1.1/";

        string title = opfXml.Descendants(dc + "title").FirstOrDefault()?.Value ?? fallbackName;
        string? author = opfXml.Descendants(dc + "creator").FirstOrDefault()?.Value;

        var metaElements = opfXml.Descendants()
            .Where(e => e.Name.LocalName == "meta")
            .ToList();

        string? series = metaElements.FirstOrDefault(e => e.Attribute("name")?.Value == "calibre:series")?.Attribute("content")?.Value;
        string? seriesIndex = metaElements.FirstOrDefault(e => e.Attribute("name")?.Value == "calibre:series_index")?.Attribute("content")?.Value;

        return (title, author, series, seriesIndex);
    }
}