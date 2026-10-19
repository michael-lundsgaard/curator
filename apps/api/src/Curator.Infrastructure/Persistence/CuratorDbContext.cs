using Curator.Application.Persistence;
using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Curator.Infrastructure.Persistence;

public class CuratorDbContext(DbContextOptions<CuratorDbContext> options) : DbContext(options), ICuratorDbContext
{
    // Any number works, as long as nothing else locks with it: the bytes spell "ColTree".
    private const long CollectionTreeLockKey = 0x436F6C54726565;

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionAsset> CollectionAssets => Set<CollectionAsset>();

    public async Task<IDbContextTransaction> LockCollectionTreeAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // A transaction-level advisory lock: Postgres releases it when the transaction commits or rolls back, so
            // it can't be left behind. Unlike a session-level one, it also works through Supabase's pooler in
            // transaction mode.
            await Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({CollectionTreeLockKey})", cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Trigram indexes for substring search (ADR 0005).
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CuratorDbContext).Assembly);
    }
}
