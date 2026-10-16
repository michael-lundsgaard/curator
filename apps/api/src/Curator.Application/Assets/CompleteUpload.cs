using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Application.Storage;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Assets;

/// <summary>
/// Marks a <see cref="AssetStatus.Pending"/> asset as <see cref="AssetStatus.Uploaded"/> once the original is in
/// storage. The presigned <c>PUT</c> doesn't enforce the size, so it's checked here.
/// </summary>
public static class CompleteUpload
{
    public sealed record Command(Guid AssetId);

    /// <param name="AssetId">The asset.</param>
    /// <param name="Status">Its status: <c>Uploaded</c>, or later if it was already completed.</param>
    public sealed record Response(Guid AssetId, AssetStatus Status);

    public sealed class Handler(ICuratorDbContext db, IFileStorage storage, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            var asset = await db.Assets.SingleOrDefaultAsync(
                a => a.Id == command.AssetId && a.DeletedAt == null, cancellationToken);
            if (asset is null)
            {
                return Error.NotFound("The asset doesn't exist.");
            }

            if (asset.UploadedBy != user.Id)
            {
                return Error.Forbidden("Only the uploader can complete an upload.");
            }

            // Completing twice is fine, so the browser can retry when a response gets lost.
            if (asset.Status != AssetStatus.Pending)
            {
                return new Response(asset.Id, asset.Status);
            }

            var size = await storage.GetSizeAsync(asset.SourceKey!, cancellationToken);
            if (size is null)
            {
                return Error.Conflict("The file hasn't been uploaded yet.");
            }

            if (size != asset.SourceSizeBytes)
            {
                return Error.Conflict(
                    $"The uploaded file is {size} bytes, but the upload was started for {asset.SourceSizeBytes}.");
            }

            asset.MarkUploaded(time.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            return new Response(asset.Id, asset.Status);
        }
    }
}
