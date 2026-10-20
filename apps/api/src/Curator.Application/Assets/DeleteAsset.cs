using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Assets;

/// <summary>
/// Deletes an asset from the library. Its uploader and admins may delete it. This is a soft delete: the row stays
/// and its files stay in storage until a cleanup job removes them.
/// </summary>
public static class DeleteAsset
{
    public sealed record Command(Guid AssetId);

    /// <param name="Id">The asset that was deleted.</param>
    /// <param name="DeletedAt">When the clip was deleted: now.</param>
    public sealed record Response(Guid Id, DateTimeOffset DeletedAt);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            // Deleted assets are filtered out, so deleting twice is "not found".
            var asset = await db.Assets.SingleOrDefaultAsync(a => a.Id == command.AssetId, cancellationToken);

            if (asset is null || !asset.IsVisibleTo(user))
            {
                return Error.NotFound("The asset doesn't exist.");
            }

            if (!asset.CanBeEditedBy(user))
            {
                return Error.Forbidden("Only the uploader or an admin can delete the asset.");
            }

            asset.Delete(time.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            return new Response(asset.Id, asset.DeletedAt!.Value);
        }
    }
}
