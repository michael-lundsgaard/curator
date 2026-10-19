using Curator.Application.Persistence;
using Curator.Domain.Collections;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

internal static class CollectionTreeLoader
{
    /// <summary>Loads the whole tree, each collection's ID and parent, in one query.</summary>
    public static async Task<CollectionTree> LoadCollectionTreeAsync(
        this ICuratorDbContext db,
        CancellationToken cancellationToken) =>
        new(await db.Collections
            .Select(c => new { c.Id, c.ParentId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentId, cancellationToken));
}
