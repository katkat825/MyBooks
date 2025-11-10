using Amazon.S3;
using Amazon.S3.Model;

namespace MyBooks.FileService.Services;

public class CloudflareR2Client
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public CloudflareR2Client(IConfiguration config)
    {
        var section = config.GetSection("CloudflareR2");

        _bucketName = section["BucketName"];
        var s3Config = new AmazonS3Config
        {
            ServiceURL = section["ServiceUrl"],
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(
            section["AccessKey"],
            section["SecretKey"],
            s3Config
        );
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = filePath
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }
}
