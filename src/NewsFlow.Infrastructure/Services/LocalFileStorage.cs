using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(string basePath = "storage")
    {
        _basePath = basePath;
    }

    public async Task<string> UploadAsync(string bucketName, string objectKey, Stream stream, string contentType, CancellationToken ct = default)
    {
        var dir = Path.Combine(_basePath, bucketName);
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, ct);

        return objectKey;
    }

    public Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, bucketName, objectKey);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {objectKey}");

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, bucketName, objectKey);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
