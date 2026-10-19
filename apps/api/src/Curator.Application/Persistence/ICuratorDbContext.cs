using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Persistence;

/// <summary>The database as the use cases see it. Handlers query it directly with LINQ.</summary>
public interface ICuratorDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Asset> Assets { get; }
    DbSet<Collection> Collections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
