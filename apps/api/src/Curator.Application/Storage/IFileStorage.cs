namespace Curator.Application.Storage;

/// <summary>Object storage for originals, playback versions and thumbnails (Cloudflare R2).</summary>
public interface IFileStorage
{
    /// <summary>
    /// Creates a presigned URL the browser uploads to with <c>PUT</c>. The request must send the same
    /// <c>Content-Type</c> and <c>Content-Length</c>, because they are part of the signature. Storage rejects a
    /// file of any other size.
    /// </summary>
    Task<string> CreateUploadUrlAsync(
        string key,
        string contentType,
        long sizeBytes,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>Returns the size of the stored object, or <c>null</c> if there is none.</summary>
    Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken);
}
