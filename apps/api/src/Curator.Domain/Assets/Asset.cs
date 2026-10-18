using Curator.Domain.Profiles;

namespace Curator.Domain.Assets;

/// <summary>A video clip in the shared library.</summary>
public class Asset
{
    public Asset(
        Guid uploadedBy,
        string originalFilename,
        string contentType,
        long sourceSizeBytes,
        DateTimeOffset? recordedAt,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFilename);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceSizeBytes);

        Id = Guid.CreateVersion7(createdAt);
        UploadedBy = uploadedBy;
        Title = DefaultTitle(originalFilename);
        OriginalFilename = originalFilename;
        ContentType = contentType;
        Status = AssetStatus.Pending;
        SourceKey = $"assets/{Id}/original";
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

    /// <summary>
    /// Storage key of the original, derived from the ID so no user input ends up in keys. Cleared once processing
    /// succeeds, unless originals are kept.
    /// </summary>
    public string? SourceKey { get; private set; }
    public long SourceSizeBytes { get; private set; }
    public string? PlaybackKey { get; private set; }
    public long? PlaybackSizeBytes { get; private set; }
    public string? ThumbnailKey { get; private set; }
    public string? ProcessingError { get; private set; }

    /// <summary>
    /// Storage key of the file to play: the playback version, or the original until processing has created one.
    /// <c>null</c> while the upload is <see cref="AssetStatus.Pending"/>, because the original may not be there yet.
    /// </summary>
    public string? PlayableKey => Status == AssetStatus.Pending ? null : PlaybackKey ?? SourceKey;

    public long? DurationMs { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    /// <summary>When the clip was captured, not when it was uploaded.</summary>
    public DateTimeOffset? RecordedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>The original is in storage. Only a <see cref="AssetStatus.Pending"/> asset can be marked.</summary>
    public void MarkUploaded(DateTimeOffset now)
    {
        if (Status != AssetStatus.Pending)
        {
            throw new InvalidOperationException($"Only pending assets can be marked as uploaded, not {Status}.");
        }

        Status = AssetStatus.Uploaded;
        UpdatedAt = now;
    }

    /// <summary>
    /// An unfinished upload isn't in the library yet, so only its uploader can see it. Everything else is shared.
    /// </summary>
    public bool IsVisibleTo(Profile user) => Status != AssetStatus.Pending || user.Id == UploadedBy;

    /// <summary>Its uploader and admins may edit or delete it.</summary>
    public bool CanBeEditedBy(Profile user) => user.Id == UploadedBy || user.Role == ProfileRole.Admin;

    /// <summary>
    /// Replaces the details people edit. The title is required; a blank description is stored as none.
    /// </summary>
    public void Edit(string title, string? description, DateTimeOffset? recordedAt, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RecordedAt = recordedAt;
        UpdatedAt = now;
    }

    public void Delete(DateTimeOffset now)
    {
        DeletedAt ??= now;
        UpdatedAt = now;
    }

    /// <summary>The filename without its extension: "Funny moment.mp4" becomes "Funny moment".</summary>
    private static string DefaultTitle(string filename)
    {
        var title = Path.GetFileNameWithoutExtension(filename).Trim();
        return title.Length > 0 ? title : filename;
    }
}
