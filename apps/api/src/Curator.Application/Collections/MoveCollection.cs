using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Moves a collection, with its sub-collections, inside another one or to the top level. Its creator and admins may
/// move it, into any collection, as long as it doesn't end up inside itself or deeper than
/// <see cref="Collection.MaxDepth"/>.
/// </summary>
/// <remarks>
/// The checks run under the collection tree lock. Without it, two moves at the same moment could both pass their
/// checks and together make a cycle (A into B while B goes into A).
/// </remarks>
public static class MoveCollection
{
    /// <param name="ParentId">The collection to move it into, or <c>null</c> for the top level.</param>
    public sealed record Command(Guid? ParentId);

    public sealed record Response(Guid Id, Guid? ParentId, DateTimeOffset UpdatedAt);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid collectionId,
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            await using var transaction = await db.LockCollectionTreeAsync(cancellationToken);

            var collection = await db.Collections.SingleOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

            if (collection is null)
            {
                return Error.NotFound("The collection doesn't exist.");
            }

            if (!collection.CanBeEditedBy(user))
            {
                return Error.Forbidden("Only the creator or an admin can move the collection.");
            }

            var tree = await db.LoadCollectionTreeAsync(cancellationToken);

            if (command.ParentId is { } parentId)
            {
                if (!tree.Contains(parentId))
                {
                    return Error.Invalid("The parent collection doesn't exist.");
                }

                if (tree.IsWithin(parentId, collection.Id))
                {
                    return Error.Conflict("A collection can't be moved into itself or one of its sub-collections.");
                }
            }

            if (!tree.CanMove(collection.Id, command.ParentId))
            {
                return Error.Conflict(
                    $"Collections can be nested {Collection.MaxDepth} levels deep, and the collection with its " +
                    "sub-collections doesn't fit there.");
            }

            collection.MoveTo(command.ParentId, time.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new Response(collection.Id, collection.ParentId, collection.UpdatedAt);
        }
    }
}
