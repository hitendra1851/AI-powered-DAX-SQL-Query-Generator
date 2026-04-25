using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Services;

public class BlobStorageService(IConfiguration configuration) : IStorageService
{
    private readonly string _connectionString = configuration["Azure:StorageConnectionString"]
        ?? throw new InvalidOperationException("Azure:StorageConnectionString is required");

    public async Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var containerName = $"tenant-{tenantId:N}";
        var client = new BlobContainerClient(_connectionString, containerName);
        await client.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobName = $"{Guid.NewGuid():N}/{fileName}";
        var blobClient = client.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(fileUrl));
        var response = await blobClient.DownloadAsync(ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(fileUrl));
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
