using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Services;

public class S3StorageService(IConfiguration configuration) : IStorageService
{
    private readonly string _bucketName = configuration["AWS:SchemaBucket"]
        ?? throw new InvalidOperationException("AWS:SchemaBucket is required");

    private readonly AmazonS3Client _s3 = new(Amazon.RegionEndpoint.GetBySystemName(
        configuration["AWS:Region"] ?? "us-east-1"));

    public async Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var key = $"tenants/{tenantId:N}/{Guid.NewGuid():N}/{fileName}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        }, ct);

        return $"s3://{_bucketName}/{key}";
    }

    public async Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var (bucket, key) = ParseS3Url(fileUrl);
        var response = await _s3.GetObjectAsync(bucket, key, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var (bucket, key) = ParseS3Url(fileUrl);
        await _s3.DeleteObjectAsync(bucket, key, ct);
    }

    private static (string bucket, string key) ParseS3Url(string url)
    {
        // Handles: s3://bucket/key  or  https://bucket.s3.region.amazonaws.com/key
        if (url.StartsWith("s3://"))
        {
            var path = url[5..];
            var slash = path.IndexOf('/');
            return (path[..slash], path[(slash + 1)..]);
        }

        var uri = new Uri(url);
        var hostParts = uri.Host.Split('.');
        return (hostParts[0], uri.AbsolutePath.TrimStart('/'));
    }
}
