using System.IO.Compression;
using System.Text;

namespace MyBooks.FileService.Tests.Infrastructure;

/// <summary>
/// Minimal but genuinely well-formed documents. The validator parses these for real, so
/// a handful of fake bytes would not exercise the code that matters.
/// </summary>
public static class SampleFiles
{
    public static byte[] MinimalPdf()
    {
        // A single-page PDF assembled by hand so the test suite has no binary fixtures.
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                  "/Contents 4 0 R /Resources << >> >>\nendobj\n");
        sb.Append("4 0 obj\n<< /Length 44 >>\nstream\n" +
                  "BT /F1 24 Tf 100 700 Td (Hello Books) Tj ET\nendstream\nendobj\n");

        var body = sb.ToString();
        var xrefOffset = Encoding.ASCII.GetByteCount(body);

        var trailer = new StringBuilder();
        trailer.Append("xref\n0 5\n");
        trailer.Append("0000000000 65535 f \n");
        for (var i = 0; i < 4; i++) trailer.Append("0000000009 00000 n \n");
        trailer.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n");
        trailer.Append(xrefOffset).Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(body + trailer);
    }

    public static byte[] MinimalEpub(
        bool includeContainer = true,
        bool includeRootfile = true,
        bool includeOpf = true,
        string opfPath = "OEBPS/content.opf",
        string title = "The Test Book",
        string author = "A. Tester")
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var mimetype = zip.CreateEntry("mimetype");
            using (var writer = new StreamWriter(mimetype.Open()))
                writer.Write("application/epub+zip");

            if (includeContainer)
            {
                var container = zip.CreateEntry("META-INF/container.xml");
                using var writer = new StreamWriter(container.Open());
                writer.Write(includeRootfile
                    ? "<?xml version=\"1.0\"?>" +
                      "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
                      $"<rootfiles><rootfile full-path=\"{opfPath}\" " +
                      "media-type=\"application/oebps-package+xml\"/></rootfiles></container>"
                    : "<?xml version=\"1.0\"?>" +
                      "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
                      "<rootfiles></rootfiles></container>");
            }

            if (includeOpf)
            {
                var opf = zip.CreateEntry(opfPath);
                using var writer = new StreamWriter(opf.Open());
                writer.Write("<?xml version=\"1.0\"?>" +
                    "<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\">" +
                    "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
                    $"<dc:title>{title}</dc:title><dc:creator>{author}</dc:creator>" +
                    "</metadata></package>");
            }
        }

        return buffer.ToArray();
    }

    public static byte[] Corrupt() => Encoding.ASCII.GetBytes("this is definitely not a document");

    public static Stream AsStream(byte[] bytes) => new MemoryStream(bytes, writable: false);
}
