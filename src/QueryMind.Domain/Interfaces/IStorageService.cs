namespace QueryMind.Domain.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}
