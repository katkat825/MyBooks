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
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
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
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] General Exception: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            Console.WriteLine($"[CloudflareR2Client] Deleted {key} from R2 (HTTP {response.HttpStatusCode}).");
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] AmazonS3Exception on delete: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] General Exception on delete: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UploadFileAsync(string key, Stream fileStream, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType
            };

            var response = await _s3Client.PutObjectAsync(request);
            Console.WriteLine($"[CloudflareR2Client] Uploaded {key} (HTTP {response.HttpStatusCode})");
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CloudflareR2Client] Upload error: {ex.Message}");
            return false;
        }
    }
}
