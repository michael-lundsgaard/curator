using Curator.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Lists the whole collection tree as a flat list, for the app to build its sidebar from. Not paged: a thousand
/// collections are a few hundred KB of JSON, fetched once (see roadmap, Collections).
/// </summary>
public static class ListCollections
{
    public sealed record Query;

    /// <param name="Items">Every collection, sorted by name ignoring case.</param>
    public sealed record Response(IReadOnlyList<Item> Items);

    /// <param name="ParentId">The collection this one is inside, or <c>null</c> at the top level.</param>
    /// <param name="ClipCount">
    /// How many clips were added to this collection itself, not counting its sub-collections' or deleted ones.
    /// </param>
    public sealed record Item(Guid Id, Guid? ParentId, string Name, int ClipCount);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var items = await db.Collections
                .OrderBy(c => c.Name.ToLower())
                .ThenBy(c => c.Id)
                .Select(c => new Item(
                    c.Id,
                    c.ParentId,
                    c.Name,
                    // Going through Assets applies its filter, so deleted clips aren't counted.
                    db.CollectionAssets.Count(ca =>
                        ca.CollectionId == c.Id && db.Assets.Any(a => a.Id == ca.AssetId))))
                .ToListAsync(cancellationToken);

            return new Response(items);
        }
    }
}
