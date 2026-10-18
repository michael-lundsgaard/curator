using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Application.Storage;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Assets;

/// <summary>
/// Gets one asset with signed URLs for playing it and showing its thumbnail. A <see cref="AssetStatus.Pending"/>
/// asset is only visible to its uploader, as in the library list.
/// </summary>
public static class GetAsset
{
    /// <summary>
    /// Long enough to watch a clip and seek in it with one URL. The app fetches the asset again for new URLs.
    /// </summary>
    public static readonly TimeSpan UrlLifetime = TimeSpan.FromHours(1);

    public sealed record Query(Guid AssetId);

    /// <param name="OriginalFilename">Name of the uploaded file on the uploader's device.</param>
    /// <param name="ContentType">MIME type of the original.</param>
    /// <param name="UploadedBy">Who uploaded the clip.</param>
    /// <param name="RecordedAt">When the clip was captured, if known.</param>
    /// <param name="CreatedAt">When the clip was uploaded.</param>
    /// <param name="UpdatedAt">When the clip last changed.</param>
    /// <param name="PlaybackUrl">
    /// Signed URL of the video to play: the playback version, or the original until processing has created one.
    /// <c>null</c> while the upload is pending.
    /// </param>
    /// <param name="ThumbnailUrl">Signed URL of the thumbnail. <c>null</c> until processing has created one.</param>
    /// <param name="UrlsExpireAt">When the signed URLs stop working.</param>
    public sealed record Response(
        Guid Id,
        string Title,
        string? Description,
        string OriginalFilename,
        string ContentType,
        AssetStatus Status,
        Uploader UploadedBy,
        DateTimeOffset? RecordedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        long? DurationMs,
        int? Width,
        int? Height,
        string? PlaybackUrl,
        string? ThumbnailUrl,
        DateTimeOffset UrlsExpireAt);

    public sealed class Handler(ICuratorDbContext db, IFileStorage storage, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(Query query, Profile user, CancellationToken cancellationToken)
        {
            var row = await db.Assets
                .AsNoTracking()
                .Where(a => a.Id == query.AssetId)
                .Select(a => new
                {
                    Asset = a,
                    UploaderName = db.Profiles.Where(p => p.Id == a.UploadedBy).Select(p => p.DisplayName).Single(),
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (row is null || !row.Asset.IsVisibleTo(user))
            {
                return Error.NotFound("The asset doesn't exist.");
            }

            var asset = row.Asset;
            var expiresAt = time.GetUtcNow() + UrlLifetime;

            return new Response(
                asset.Id,
                asset.Title,
                asset.Description,
                asset.OriginalFilename,
                asset.ContentType,
                asset.Status,
                new Uploader(asset.UploadedBy, row.UploaderName),
                asset.RecordedAt,
                asset.CreatedAt,
                asset.UpdatedAt,
                asset.DurationMs,
                asset.Width,
                asset.Height,
                await SignAsync(asset.PlayableKey, expiresAt, cancellationToken),
                await SignAsync(asset.ThumbnailKey, expiresAt, cancellationToken),
                expiresAt);
        }

        private async Task<string?> SignAsync(string? key, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
            key is null ? null : await storage.CreateDownloadUrlAsync(key, expiresAt, cancellationToken);
    }
}
