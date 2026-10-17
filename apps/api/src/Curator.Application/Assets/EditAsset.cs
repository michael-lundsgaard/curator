using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Assets;

/// <summary>
/// Edits the title, description and recording time of an asset. Its uploader and admins may edit it. The body
/// replaces all three, so a missing description or recording time clears it.
/// </summary>
public static class EditAsset
{
    public const int MaxTitleLength = 255;
    public const int MaxDescriptionLength = 5000;

    /// <param name="Title">The new title. Required.</param>
    /// <param name="Description">The new description, or <c>null</c> for none.</param>
    /// <param name="RecordedAt">When the clip was captured, or <c>null</c> if unknown.</param>
    public sealed record Command(
        [Required, MaxLength(MaxTitleLength)] string Title,
        [MaxLength(MaxDescriptionLength)] string? Description,
        DateTimeOffset? RecordedAt);

    /// <param name="UpdatedAt">When the clip last changed: now.</param>
    public sealed record Response(
        Guid Id,
        string Title,
        string? Description,
        DateTimeOffset? RecordedAt,
        DateTimeOffset UpdatedAt);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid assetId,
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == assetId, cancellationToken);

            // Someone else's unfinished upload doesn't exist for them, as in GetAsset.
            if (asset is null || (asset.Status == AssetStatus.Pending && asset.UploadedBy != user.Id))
            {
                return Error.NotFound("The asset doesn't exist.");
            }

            if (!asset.CanBeEditedBy(user))
            {
                return Error.Forbidden("Only the uploader or an admin can edit the asset.");
            }

            asset.Edit(command.Title, command.Description, command.RecordedAt, time.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            return new Response(asset.Id, asset.Title, asset.Description, asset.RecordedAt, asset.UpdatedAt);
        }
    }
}
