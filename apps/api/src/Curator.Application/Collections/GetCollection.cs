using Curator.Application.Common;
using Curator.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>Gets one collection. Collections are shared, so everyone can see every one.</summary>
public static class GetCollection
{
    public sealed record Query(Guid CollectionId);

    /// <param name="Id">The collection.</param>
    /// <param name="ParentId">The collection this one is inside, or <c>null</c> at the top level.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Description">What the collection is for, or <c>null</c> for none.</param>
    /// <param name="CreatedBy">Who created the collection.</param>
    /// <param name="CreatedAt">When the collection was created.</param>
    /// <param name="UpdatedAt">When the collection last changed: its name, description or place in the tree.</param>
    public sealed record Response(
        Guid Id,
        Guid? ParentId,
        string Name,
        string? Description,
        Creator CreatedBy,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var response = await db.Collections
                .Where(c => c.Id == query.CollectionId)
                .Select(c => new Response(
                    c.Id,
                    c.ParentId,
                    c.Name,
                    c.Description,
                    new Creator(
                        c.CreatedBy,
                        db.Profiles.Where(p => p.Id == c.CreatedBy).Select(p => p.DisplayName).Single()),
                    c.CreatedAt,
                    c.UpdatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return response is null ? Error.NotFound("The collection doesn't exist.") : response;
        }
    }
}
