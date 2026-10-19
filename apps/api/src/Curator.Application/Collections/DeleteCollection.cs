using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Deletes a collection and all of its sub-collections, for good (the database cascades). Its creator and admins may
/// delete it, even when the sub-collections are by others. Clips stay in the library.
/// </summary>
public static class DeleteCollection
{
    public sealed record Command(Guid CollectionId);

    /// <param name="DeletedCount">How many collections were deleted: this one and its sub-collections.</param>
    public sealed record Response(Guid Id, int DeletedCount);

    public sealed class Handler(ICuratorDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            var collection = await db.Collections
                .SingleOrDefaultAsync(c => c.Id == command.CollectionId, cancellationToken);

            if (collection is null)
            {
                return Error.NotFound("The collection doesn't exist.");
            }

            if (!collection.CanBeEditedBy(user))
            {
                return Error.Forbidden("Only the creator or an admin can delete the collection.");
            }

            var tree = await db.LoadCollectionTreeAsync(cancellationToken);
            var deletedCount = tree.CountWithin(collection.Id);

            db.Collections.Remove(collection);
            await db.SaveChangesAsync(cancellationToken);

            return new Response(collection.Id, deletedCount);
        }
    }
}
