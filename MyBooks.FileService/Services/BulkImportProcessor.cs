using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.IO.Compression;
using System.Management;
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

    public BulkImportProcessor(
        FileDbContext context,
        GoogleDriveClient googleDriveClient,
        SystemTokenHelper tokenHelper,
        HttpClient httpClient,
        IConfiguration config,
        HtmlSanitizationService sanitizer)
    {
        _context = context;
        _googleDriveClient = googleDriveClient;
        _tokenHelper = tokenHelper;
        _httpClient = httpClient;
        _config = config;
        _sanitizer = sanitizer;
    }

    public async Task ProcessJobAsync(int jobId, FileScanDto scanDto)
    {
        Console.WriteLine($"[BulkImport] Starting job {jobId} for tenant {scanDto.TenantId} with {scanDto.BulkImportStart.FileIds.Count} files.");

        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null || (job.Status != "Pending" && job.Status != "RetryFails"))
        {
            Console.WriteLine($"[BulkImport] Job {jobId} not found.");
            return;
        }

        job.Status = "Running";
        await _context.SaveChangesAsSystemAsync();
        Console.WriteLine($"[BulkImport] Job {jobId} marked as Running.");

        var integration = await _context.GoogleIntegrations
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

            Console.WriteLine("integration = null. failed");

            await _context.SaveChangesAsSystemAsync();
            return;
        }

        Console.WriteLine("sending for 1st run");

        // process items - first run
        await ProcessItemsAsync(job, integration, scanDto);

        Console.WriteLine("1st run processed");

        if (job.Items.All(i => i.Status == "Success"))
        {
            job.Status = "Completed";
            Console.WriteLine("Success on the first run");
        }

        else if (job.Items.All(i => i.Status == "Failed"))
        {
            job.Status = "Failed";
            Console.WriteLine("all fail on the first run");
        }

        else
        {
            job.Status = "RetryFails";

            await _context.SaveChangesAsSystemAsync();

            foreach (var item in job.Items.Where(i => i.Status == "Failed"))
            {
                item.Status = "Pending";
                item.ErrorMessage = null;
                job.ProcessedFiles--; // remove failed items from processed count. they'll be processed again
            }

            Console.WriteLine("2nd try being called now");

            await _context.SaveChangesAsSystemAsync();

            // retry fails - second run
            await ProcessItemsAsync(job, integration, scanDto);

            if (job.Items.All(i => i.Status == "Success"))
                job.Status = "Completed";
            else
                job.Status = "CompletedWithErrors";
        }

        await _context.SaveChangesAsSystemAsync();
    }

    private async Task ProcessItemsAsync(BulkImportJob job, GoogleIntegration integration, FileScanDto scanDto)
    {
        Console.WriteLine("processing items");

        foreach (var item in job.Items.Where(i => i.Status == "Pending"))
        {
            Console.WriteLine($"[BulkImport] Processing file {item.FileId}...");
            try
            {
                var file = await _googleDriveClient.GetFileAsync(item.FileId, integration.RefreshToken);
                if (file == null)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = "File not found in Google Drive";
                    continue;
                }

                Console.WriteLine("processing items - file is not null");

                item.FileName = _sanitizer.Sanitize(file.Name);

                using var stream = await _googleDriveClient.GetFileStreamAsync(item.FileId, integration.RefreshToken);

                string title;
                string? author;

                if (file.MimeType == "application/pdf")
                {
                    Console.WriteLine("sending to pdf extract method");
                    (title, author) = ExtractPdfMetadata(stream, item.FileName);
                }
                else if (file.MimeType == "application/epub+zip")
                {
                    Console.WriteLine("sending to epub extract method");
                    (title, author) = ExtractEpubMetadata(stream, item.FileName);
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
                    TenantId = job.TenantId
                };

                Console.WriteLine("book import request dto created, sending now");

                var catalogUrl = _config["ServiceUrls:CatalogService"];
                var bookResponse = await _httpClient.PostAsJsonAsync($"{catalogUrl}/api/book-import", requestDto);
                bookResponse.EnsureSuccessStatusCode();

                var responseDto = await bookResponse.Content.ReadFromJsonAsync<BookImportResponseDto>();

                Console.WriteLine("book import response received");

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
                    CreatedBy = scanDto.UserId,
                    CreatedDate = DateTime.UtcNow
                };

                Console.WriteLine("sending filemetadata create request now");

                _context.Files.Add(fileMeta);
                await _context.SaveChangesAsSystemAsync(scanDto.UserId, scanDto.IpAddress);

                Console.WriteLine("file metadata saved, now to link the book to the file");

                item.CreatedFileId = fileMeta.Id;

                var linkDto = new BookFileLinkDto
                {
                    BookId = fileMeta.BookId,
                    FileId = fileMeta.Id
                };

                var linkResponse = await _httpClient.PatchAsJsonAsync($"{catalogUrl}/api/book-import/file", linkDto);
                linkResponse.EnsureSuccessStatusCode();

                Console.WriteLine("book successfully updated with fileid");

                item.Status = "Success";

                Console.WriteLine($"[BulkImport] File {item.FileId} processed successfully. BookId={item.CreatedBookId}, FileId={item.CreatedFileId}");
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
        Console.WriteLine("pdf extractor reached");

        fallbackName = Path.GetFileNameWithoutExtension(fallbackName);
        using var mem = new MemoryStream();
        pdfStream.CopyTo(mem);
        mem.Position = 0;

        Console.WriteLine("starting using var pdf = PdfReader.Open(mem, PdfDocumentOpenMode.ReadOnly);");

        try
        {
            using var pdf = PdfReader.Open(mem, PdfDocumentOpenMode.ReadOnly);

            Console.WriteLine("finished using var pdf = PdfReader.Open(mem, PdfDocumentOpenMode.ReadOnly);");

            string title = !string.IsNullOrWhiteSpace(pdf.Info.Title)
                ? _sanitizer.Sanitize(pdf.Info.Title)
                : fallbackName;

            string? author = !string.IsNullOrWhiteSpace(pdf.Info.Author)
                ? _sanitizer.Sanitize(pdf.Info.Author) : null;

            return (title, author);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BulkImport] WARNING: Failed to parse PDF metadata, using fallback. Error: {ex.Message}");
            return (fallbackName, null);
        }
    }

    private (string Title, string? Author) ExtractEpubMetadata(Stream epubStream, string fallbackName)
    {
        Console.WriteLine("epub extractor reached");

        fallbackName = Path.GetFileNameWithoutExtension(fallbackName);
        epubStream.Position = 0;
        using var archive = new ZipArchive(epubStream, ZipArchiveMode.Read, leaveOpen: true);

        var containerEntry = archive.GetEntry("META-INF/container.xml") ?? null;
        if (containerEntry == null)
            return (fallbackName, null);

        using var containerStream = containerEntry.Open();
        var containerXml = XDocument.Load(containerStream);

        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
        var rootfileElement = containerXml
            .Descendants(ns + "rootfile")
            .FirstOrDefault()
            ?? null;

        if (rootfileElement == null)
            return (fallbackName, null);

        var fullPath = rootfileElement.Attribute("full-path")?.Value;
        if (string.IsNullOrWhiteSpace(fullPath))
            return (fallbackName, null);

        var opfEntry = archive.GetEntry(fullPath) ?? null;
        if (opfEntry == null)
            return (fallbackName, null);

        using var opfStream = opfEntry.Open();
        var opfXml = XDocument.Load(opfStream);

        XNamespace dc = "http://purl.org/dc/elements/1.1/";

        string title = opfXml.Descendants(dc + "title").FirstOrDefault()?.Value ?? fallbackName;
        string? author = opfXml.Descendants(dc + "creator").FirstOrDefault()?.Value;

        return (title, author);
    }
}