namespace Curator.Domain.Assets;

/// <summary>A video clip in the shared library.</summary>
public class Asset
{
    public Asset(
        Guid uploadedBy,
        string title,
        string originalFilename,
        string contentType,
        string sourceKey,
        long sourceSizeBytes,
        DateTimeOffset? recordedAt,
        DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7(createdAt);
        UploadedBy = uploadedBy;
        Title = title;
        OriginalFilename = originalFilename;
        ContentType = contentType;
        Status = AssetStatus.Pending;
        SourceKey = sourceKey;
        SourceSizeBytes = sourceSizeBytes;
        RecordedAt = recordedAt;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UploadedBy { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string OriginalFilename { get; private set; }
    public string ContentType { get; private set; }
    public AssetStatus Status { get; private set; }

    /// <summary>Storage key of the original. Cleared once processing succeeds, unless originals are kept.</summary>
    public string? SourceKey { get; private set; }
    public long SourceSizeBytes { get; private set; }
    public string? PlaybackKey { get; private set; }
    public long? PlaybackSizeBytes { get; private set; }
    public string? ThumbnailKey { get; private set; }
    public string? ProcessingError { get; private set; }

    public long? DurationMs { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    /// <summary>When the clip was captured, not when it was uploaded.</summary>
    public DateTimeOffset? RecordedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void Delete(DateTimeOffset now)
    {
        DeletedAt ??= now;
        UpdatedAt = now;
    }
}
