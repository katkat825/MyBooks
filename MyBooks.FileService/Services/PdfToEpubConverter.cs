using System.Diagnostics;

namespace MyBooks.FileService.Services;

public class PdfToEpubConverter
{
    private readonly CloudflareR2Client _r2Client;
    private readonly string _tempDir;

    public PdfToEpubConverter(CloudflareR2Client r2Client)
    {
        _r2Client = r2Client;
        _tempDir = Path.Combine(Path.GetTempPath(), "mybooks");
        Directory.CreateDirectory(_tempDir);
    }

    public async Task<string?> ConvertAndUploadAsync(Stream pdfStream, string tenantId, string fileNameWithoutExt)
    {
        string tempPdfPath = Path.Combine(_tempDir, $"{fileNameWithoutExt}.pdf");
        string tempEpubPath = Path.Combine(_tempDir, $"{fileNameWithoutExt}.epub");
        string r2Key = $"{tenantId}/{fileNameWithoutExt}.epub";

        try
        {
            // write pdf to temp
            using (var fs = new FileStream(tempPdfPath, FileMode.Create, FileAccess.Write))
            {
                await pdfStream.CopyToAsync(fs);
            }

            // run calibre CLI
            var psi = new ProcessStartInfo
            {
                FileName = "ebook-convert",
                Arguments = $"\"{tempPdfPath}\" \"{tempEpubPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            string stdOut = await process.StandardOutput.ReadToEndAsync();
            string stdErr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(tempEpubPath))
            {
                Console.WriteLine($"[PdfToEpubConverter] Conversion failed: ExitCode={process.ExitCode}, Error={stdErr}");
                TryDeleteFile(tempPdfPath);
                TryDeleteFile(tempEpubPath);
                return null;
            }

            Console.WriteLine($"[PdfToEpubConverter] Conversion succeeded: {fileNameWithoutExt}.epub");

            // upload to cloudflare
            using var epubStream = new FileStream(tempEpubPath, FileMode.Open, FileAccess.Read);
            bool uploadOk = await _r2Client.UploadFileAsync(r2Key, epubStream, "application/epub+zip");

            // cleanup temp files
            TryDeleteFile(tempPdfPath);
            TryDeleteFile(tempEpubPath);

            if (!uploadOk)
            {
                Console.WriteLine($"[PdfToEpubConverter] Upload failed for {r2Key}, cleaning up R2 object if any");
                await _r2Client.DeleteFileAsync(r2Key);
                return null;
            }

            Console.WriteLine($"[PdfToEpubConverter] Upload complete: {r2Key}");
            return r2Key;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PdfToEpubConverter] Exception: {ex.Message}");
            await _r2Client.DeleteFileAsync(r2Key);
            return null;
        }
        finally
        {
            TryDeleteFile(tempPdfPath);
            TryDeleteFile(tempEpubPath);
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Console.WriteLine($"[PdfToEpubConverter] Deleted temp file: {path}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PdfToEpubConverter] Failed to delete temp file {path}: {ex.Message}");
        }
    }
}
