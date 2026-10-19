using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Curator.Application.Persistence;

/// <summary>The database as the use cases see it. Handlers query it directly with LINQ.</summary>
public interface ICuratorDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Asset> Assets { get; }
    DbSet<Collection> Collections { get; }
    DbSet<CollectionAsset> CollectionAssets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a transaction and waits until no other request is changing the collection tree, so checks made against
    /// the tree still hold when the change is saved. Commit the transaction after saving; disposing it without
    /// committing rolls back. Either releases the lock.
    /// </summary>
    Task<IDbContextTransaction> LockCollectionTreeAsync(CancellationToken cancellationToken);
}
