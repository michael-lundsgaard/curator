using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Removes clips from a collection, and optionally from its sub-collections. Collections are shared, so everyone can
/// remove clips from any of them. Clips that aren't in it are skipped, so removing twice is harmless. The clips stay in
/// the library and in their other collections.
/// </summary>
public static class RemoveClipsFromCollection
{
    /// <param name="IncludeSubCollections">
    /// Also remove the clips from its sub-collections, at every level, as listed by <c>GET /assets</c> with
    /// <c>includeSubCollections</c>.
    /// </param>
    public sealed record Command(
        [Required, Length(1, AddClipsToCollection.MaxClips)] IReadOnlyList<Guid> AssetIds,
        bool IncludeSubCollections = false);

    /// <param name="RemovedCount">
    /// How many clips were removed, not counting those that weren't in it. A clip removed from several sub-collections
    /// counts once.
    /// </param>
    public sealed record Response(Guid Id, int RemovedCount);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid collectionId,
            Command command,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Guid> collectionIds;
            if (command.IncludeSubCollections)
            {
                var tree = await db.LoadCollectionTreeAsync(cancellationToken);
                if (!tree.Contains(collectionId))
                {
                    return Error.NotFound("The collection doesn't exist.");
                }

                collectionIds = tree.IdsWithin(collectionId);
            }
            else
            {
                if (!await db.Collections.AnyAsync(c => c.Id == collectionId, cancellationToken))
                {
                    return Error.NotFound("The collection doesn't exist.");
                }

                collectionIds = [collectionId];
            }

            var rows = db.CollectionAssets
                .Where(ca => collectionIds.Contains(ca.CollectionId) && command.AssetIds.Contains(ca.AssetId));

            // Counted before the delete, which returns rows, not clips. A remove of the same clips at the same moment
            // can make both count them; the clips are gone either way.
            var removed = await rows.Select(ca => ca.AssetId).Distinct().CountAsync(cancellationToken);
            await rows.ExecuteDeleteAsync(cancellationToken);

            return new Response(collectionId, removed);
        }
    }
}
