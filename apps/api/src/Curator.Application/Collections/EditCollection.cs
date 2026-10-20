using System.ComponentModel.DataAnnotations;
using Curator.Application.Common;
using Curator.Application.Persistence;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Collections;

/// <summary>
/// Edits the name and description of a collection. Its creator and admins may edit it. The body replaces both, so a
/// missing description clears it. Moving is <see cref="MoveCollection"/>.
/// </summary>
public static class EditCollection
{
    /// <param name="Name">The new name.</param>
    /// <param name="Description">The new description, or <c>null</c> for none.</param>
    public sealed record Command(
        [Required, MaxLength(CreateCollection.MaxNameLength)] string Name,
        [MaxLength(CreateCollection.MaxDescriptionLength)] string? Description);

    /// <param name="Id">The collection.</param>
    /// <param name="Name">The new name.</param>
    /// <param name="Description">The new description, or <c>null</c> for none.</param>
    /// <param name="UpdatedAt">When the collection last changed: now.</param>
    public sealed record Response(Guid Id, string Name, string? Description, DateTimeOffset UpdatedAt);

    public sealed class Handler(ICuratorDbContext db, TimeProvider time)
    {
        public async Task<Result<Response>> HandleAsync(
            Guid collectionId,
            Command command,
            Profile user,
            CancellationToken cancellationToken)
        {
            var collection = await db.Collections.SingleOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

            if (collection is null)
            {
                return Error.NotFound("The collection doesn't exist.");
            }

            if (!collection.CanBeEditedBy(user))
            {
                return Error.Forbidden("Only the creator or an admin can edit the collection.");
            }

            collection.Edit(command.Name, command.Description, time.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            return new Response(collection.Id, collection.Name, collection.Description, collection.UpdatedAt);
        }
    }
}
