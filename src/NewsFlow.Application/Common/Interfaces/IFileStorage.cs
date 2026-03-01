namespace NewsFlow.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default);
}
