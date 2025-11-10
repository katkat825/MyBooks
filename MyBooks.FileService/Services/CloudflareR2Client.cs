using Amazon.S3;
using Amazon.S3.Model;

namespace MyBooks.FileService.Services;

public class CloudflareR2Client
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _serviceUrl;

    public CloudflareR2Client(IConfiguration config)
    {
        var section = config.GetSection("CloudflareR2");

        _bucketName = section["BucketName"];
        _serviceUrl = section["ServiceUrl"];

        Console.WriteLine($"[CloudflareR2Client] Initializing R2 client:");
        Console.WriteLine($"  BucketName: {_bucketName}");
        Console.WriteLine($"  ServiceUrl: {_serviceUrl}");
        Console.WriteLine($"  Using AccessKey: {section["AccessKey"]?.Substring(0, Math.Min(6, section["AccessKey"]?.Length ?? 0))}****");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = _serviceUrl,
            ForcePathStyle = true,
            UseHttp = true
        };

        _s3Client = new AmazonS3Client(
            section["AccessKey"],
            section["SecretKey"],
            s3Config
        );

        Console.WriteLine("[CloudflareR2Client] R2 client successfully configured.");
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        Console.WriteLine($"[CloudflareR2Client] Attempting to fetch file from R2:");
        Console.WriteLine($"  Bucket: {_bucketName}");
        Console.WriteLine($"  Key: {filePath}");

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = filePath
            };

            var response = await _s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] AmazonS3Exception: {ex.Message}");
            Console.WriteLine($"  StatusCode: {ex.StatusCode}");
            Console.WriteLine($"  RequestId: {ex.RequestId}");
            Console.WriteLine($"  ErrorCode: {ex.ErrorCode}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] General Exception: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
