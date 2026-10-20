using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Creates a collection, at the top level or inside another one. Everyone can create one inside any collection,
/// as long as the tree doesn't get deeper than <see cref="Collection.MaxDepth"/>.
/// </summary>
public static class CreateCollection
{
    public const int MaxNameLength = 255;
    public const int MaxDescriptionLength = 5000;

    /// <param name="Name">The name, e.g. a game or an occasion.</param>
    /// <param name="Description">What the collection is for, or <c>null</c> for none.</param>
    /// <param name="ParentId">The collection to create it inside, or <c>null</c> for the top level.</param>
    public sealed record Command(
        [Required, MaxLength(MaxNameLength)] string Name,
        [MaxLength(MaxDescriptionLength)] string? Description,
        Guid? ParentId);

    /// <param name="Id">The new collection.</param>
    /// <param name="ParentId">The collection it was created inside, or <c>null</c> at the top level.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Description">The description, or <c>null</c> for none.</param>
    /// <param name="CreatedAt">When the collection was created: now.</param>
    public sealed record Response(
        Guid Id,
        Guid? ParentId,
        string Name,
        string? Description,
        DateTimeOffset CreatedAt);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            // Inside a parent, the depth and the parent itself are checked under the lock, so a move or delete running
            // at the same moment can't invalidate them before the save. A top-level collection depends on nothing.
            await using var transaction = command.ParentId is null
                ? null
                : await db.LockCollectionTreeAsync(cancellationToken);

            if (command.ParentId is { } parentId)
            {
                var tree = await db.LoadCollectionTreeAsync(cancellationToken);

                if (!tree.Contains(parentId))
                {
                    return Error.Invalid("The parent collection doesn't exist.");
                }

                if (!tree.CanAddChildTo(parentId))
                {
                    return Error.Conflict(
                        $"Collections can be nested {Collection.MaxDepth} levels deep, and the parent is at the last.");
                }
            }

            var collection = new Collection(
                user.Id, command.ParentId, command.Name, command.Description, time.GetUtcNow());

            db.Collections.Add(collection);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new Response(
                collection.Id, collection.ParentId, collection.Name, collection.Description, collection.CreatedAt);
        }
    }
}
