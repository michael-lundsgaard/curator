using Curator.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace Curator.Application.Persistence;

/// <summary>The database as the use cases see it. Handlers query it directly with LINQ.</summary>
public interface ICuratorDbContext
{
    DbSet<Asset> Assets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
