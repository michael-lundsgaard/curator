using Curator.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Profiles;

/// <summary>
/// Lists everyone, for the "Uploaded by" filter. Not paged: Curator is for a small, invited group. Roles are left
/// out until the admin members list needs them (see roadmap, Library).
/// </summary>
public static class ListProfiles
{
    public sealed record Query;

    /// <param name="Items">Every profile, sorted by display name ignoring case.</param>
    public sealed record Response(IReadOnlyList<Item> Items);

    public sealed record Item(Guid Id, string DisplayName);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var items = await db.Profiles
                .OrderBy(p => p.DisplayName.ToLower())
                .ThenBy(p => p.Id)
                .Select(p => new Item(p.Id, p.DisplayName))
                .ToListAsync(cancellationToken);

            return new Response(items);
        }
    }
}
