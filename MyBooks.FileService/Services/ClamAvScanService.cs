using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyBooks.FileService.Services;

public class ClamAvScanService
{
    private readonly string _clamavHost;
    private readonly int _clamavPort;

    public ClamAvScanService(IConfiguration config)
    {
        _clamavHost = config["CLAMAV_HOST"] ?? "clamav";
        _clamavPort = int.TryParse(config["ClamAV:Port"], out var port) ? port : 3310;
    }

    public async Task<bool> IsFileCleanAsync(Stream fileStream)
    {
        try
        {
            // connect to ClamAV
            using var client = new TcpClient();
            await client.ConnectAsync(_clamavHost, _clamavPort);
            await using var network = client.GetStream();

            // send the zINSTREAM command (streaming scan mode)
            var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await network.WriteAsync(command);

            // stream file to ClamAV in chunks
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytesRead));
                await network.WriteAsync(length);
                await network.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            // signal end of stream
            await network.WriteAsync(BitConverter.GetBytes(0));

            // read response from ClamAV
            using var reader = new StreamReader(network);
            var response = await reader.ReadLineAsync() ?? string.Empty;

            // ClamAV replies with "stream: OK" if clean, or "stream: [virus] FOUND"
            return !response.Contains("FOUND", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // fail open (allow upload) if ClamAV is unavailable
            return true;
        }
        finally
        {
            // reset position for later reuse (e.g., upload to Drive/R2)
            if (fileStream.CanSeek)
                fileStream.Seek(0, SeekOrigin.Begin);
        }
    }
}
