using System.Collections.Concurrent;
using Curator.Application.Storage;

namespace Curator.IntegrationTests;

/// <summary>
/// Presigns with the real R2 implementation (computed locally), but answers questions about stored objects from
/// <paramref name="objects"/>, so tests never call R2.
/// </summary>
internal sealed class TestFileStorage(IFileStorage presigner, ConcurrentDictionary<string, long> objects) : IFileStorage
{
    public Task<string> CreateUploadUrlAsync(
        string key,
        string contentType,
        long sizeBytes,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken) =>
        presigner.CreateUploadUrlAsync(key, contentType, sizeBytes, expiresAt, cancellationToken);

    public Task<long?> GetSizeAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(objects.TryGetValue(key, out var size) ? size : (long?)null);
}
