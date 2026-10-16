using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Infrastructure.Persistence;

public class CuratorDbContext(DbContextOptions<CuratorDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CuratorDbContext).Assembly);
}
