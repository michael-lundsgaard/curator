using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Adds clips to a collection. Collections are shared, so everyone can add clips to any of them. Clips already in
/// the collection are skipped, so adding twice is harmless.
/// </summary>
/// <remarks>
/// The checks and the save run under the collection tree lock. Without it, a delete at the same moment could remove the
/// collection before the save, and two adds of the same clip could both find it missing; either would fail on a
/// database constraint instead of returning a result.
/// </remarks>
public static class AddClipsToCollection
{
    public const int MaxClips = 100;

    /// <param name="AssetIds">The clips to add, 1 to 100. They must be uploaded and not deleted.</param>
    public sealed record Command([Required, Length(1, MaxClips)] IReadOnlyList<Guid> AssetIds);

    /// <param name="AddedCount">How many clips were added, not counting those already in the collection.</param>
    public sealed record Response(Guid Id, int AddedCount);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid collectionId,
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            var assetIds = command.AssetIds.Distinct().ToList();

            await using var transaction = await db.LockCollectionTreeAsync(cancellationToken);

            if (!await db.Collections.AnyAsync(c => c.Id == collectionId, cancellationToken))
            {
                return Error.NotFound("The collection doesn't exist.");
            }

            // Deleted clips are filtered out, and pending uploads aren't in the library yet.
            var existing = await db.Assets
                .CountAsync(a => assetIds.Contains(a.Id) && a.Status != AssetStatus.Pending, cancellationToken);

            if (existing != assetIds.Count)
            {
                return Error.Invalid($"{assetIds.Count - existing} of the clips don't exist.");
            }

            var alreadyAdded = await db.CollectionAssets
                .Where(ca => ca.CollectionId == collectionId && assetIds.Contains(ca.AssetId))
                .Select(ca => ca.AssetId)
                .ToListAsync(cancellationToken);

            var now = time.GetUtcNow();
            var added = assetIds
                .Except(alreadyAdded)
                .Select(assetId => new CollectionAsset(collectionId, assetId, user.Id, now))
                .ToList();

            db.CollectionAssets.AddRange(added);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new Response(collectionId, added.Count);
        }
    }
}
