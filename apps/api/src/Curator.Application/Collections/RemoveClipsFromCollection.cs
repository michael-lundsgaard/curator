using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Removes clips from a collection. Collections are shared, so everyone can remove clips from any of them. Clips that
/// aren't in the collection are skipped, so removing twice is harmless. The clips stay in the library and in their
/// other collections.
/// </summary>
public static class RemoveClipsFromCollection
{
    /// <param name="AssetIds">The clips to remove, 1 to 100.</param>
    public sealed record Command([Required, Length(1, AddClipsToCollection.MaxClips)] IReadOnlyList<Guid> AssetIds);

    /// <param name="Id">The collection.</param>
    /// <param name="RemovedCount">How many clips were removed, not counting those that weren't in it.</param>
    public sealed record Response(Guid Id, int RemovedCount);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid collectionId,
            Command command,
            CancellationToken cancellationToken)
        {
            if (!await db.Collections.AnyAsync(c => c.Id == collectionId, cancellationToken))
            {
                return Error.NotFound("The collection doesn't exist.");
            }

            var removed = await db.CollectionAssets
                .Where(ca => ca.CollectionId == collectionId && command.AssetIds.Contains(ca.AssetId))
                .ExecuteDeleteAsync(cancellationToken);

            return new Response(collectionId, removed);
        }
    }
}
