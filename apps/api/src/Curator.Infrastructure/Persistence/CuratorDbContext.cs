using Curator.Application.Persistence;
using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Infrastructure.Persistence;

public class CuratorDbContext(DbContextOptions<CuratorDbContext> options) : DbContext(options), ICuratorDbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Collection> Collections => Set<Collection>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Trigram indexes for substring search (ADR 0005).
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CuratorDbContext).Assembly);
    }
}
