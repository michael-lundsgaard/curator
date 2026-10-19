using Curator.Domain.Profiles;
using Curator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Curator.Api.Development;

/// <summary>Adds and clears local data for the development endpoints.</summary>
public static class DevelopmentData
{
    /// <summary>
    /// Saves the <see cref="SampleData"/>, unless it's already there. Returns the number of clips and collections
    /// added.
    /// </summary>
    public static async Task<(int Clips, int Collections)> SeedAsync(
        CuratorDbContext db,
        Profile caller,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Profiles.AnyAsync(p => p.Id == SampleData.FrejaId, cancellationToken))
        {
            return (0, 0);
        }

        var sample = new SampleData(caller.Id, now);
        db.Profiles.AddRange(sample.People);
        db.Collections.AddRange(sample.Collections);
        db.Assets.AddRange(sample.Clips);
        db.Assets.AddRange(sample.AbandonedUploads);
        db.CollectionAssets.AddRange(sample.CollectionClips);

        await db.SaveChangesAsync(cancellationToken);
        return (sample.Clips.Count, sample.Collections.Count);
    }

    /// <summary>
    /// Deletes every asset, including deleted and pending ones, every collection and the sample people. Real users'
    /// profiles stay, with their roles. Files in storage are left alone. Returns the number of assets deleted.
    /// </summary>
    public static async Task<int> ClearAsync(CuratorDbContext db, CancellationToken cancellationToken)
    {
        if (!IsLocalDatabase(db))
        {
            throw new InvalidOperationException("Only a database on this machine can be cleared.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var deleted = await db.Assets.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        await db.Collections.ExecuteDeleteAsync(cancellationToken);
        await db.Profiles
            .Where(p => p.Id == SampleData.FrejaId || p.Id == SampleData.MadsId)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    /// <summary>
    /// Whether the database runs on this machine (Docker or Testcontainers). A second lock on clearing, besides the
    /// Development environment, so a misconfigured server can't wipe the hosted database.
    /// </summary>
    public static bool IsLocalDatabase(CuratorDbContext db)
    {
        var host = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Host;
        return host is "localhost" or "127.0.0.1" or "::1";
    }
}
