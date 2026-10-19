using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Curator.Infrastructure.Persistence;

/// <summary>
/// Sample profiles, clips and collections for trying the API locally (Development only). The clips have no files in
/// storage, so nothing is uploaded to R2 and nothing is left behind there.
/// </summary>
public static class SampleData
{
    private static readonly Guid FrejaId = new("0199f0a0-0000-7000-8000-000000000001");
    private static readonly Guid MadsId = new("0199f0a0-0000-7000-8000-000000000002");

    // Title, description, days ago it was recorded (null: unknown), uploader (0: the caller, 1: Freja, 2: Mads).
    private static readonly (string Title, string? Description, int? RecordedDaysAgo, int Uploader)[] Clips =
    [
        ("R.E.P.O - the scream", "Mads finds the monster first", 1, 2),
        ("Funny moment in the truck", null, 1, 1),
        ("Lethal Company: jumpscare", "Coil-head behind the door", 3, 0),
        ("Øl hos Søren", "Skål!", 4, 1),
        ("Phasmophobia ghost hunt", "Night two in the haunted house", 6, 2),
        ("Phasmophobia: the ghost photo", null, 6, 0),
        ("Best of October", "Compilation", 9, 1),
        ("Speedrun attempt 3", null, 12, 2),
        ("100% completion!", "Finally", 14, 0),
        ("Content Warning: we went viral", null, 15, 1),
        ("Ærlig talt, hvad skete der", null, 18, 2),
        ("Boss fight fail", "So close", 21, 1),
        ("Boss fight win", "Third time lucky", 21, 2),
        ("Dancing NPC", null, null, 1),
        ("Lag spike of the century", null, 30, 0),
        ("Hide and seek", "Nobody found Freja", 34, 2),
        ("Winter holiday 2025: snow fight", null, 300, 1),
        ("Old clip from the archive", "Recorded before Curator existed", 700, 2),
    ];

    // Name, description, parent's index in this list (null: top level), creator (as for clips). Parents come first.
    private static readonly (string Name, string? Description, int? Parent, int Creator)[] Collections =
    [
        ("R.E.P.O", "Monster-filled evenings", null, 2),
        ("Phasmophobia", null, null, 0),
        ("Haunted house", "Two nights in a row", 1, 2),
        ("Lethal Company", null, null, 1),
        ("Best of", "The clips worth showing others", null, 1),
        ("2025", null, 4, 1),
        ("2026", null, 4, 0),
        ("Screams", null, 6, 2),
        ("Fails", null, 6, 1),
        ("hangouts", "Lowercase on purpose: collections sort by name ignoring case", null, 0),
    ];

    /// <summary>Number of clips added, all listed in the library.</summary>
    public static int ClipCount => Clips.Length;

    /// <summary>Number of those clips uploaded by the caller.</summary>
    public static int CallerClipCount => Clips.Count(c => c.Uploader == 0);

    /// <summary>Number of collections added, in a tree three levels deep.</summary>
    public static int CollectionCount => Collections.Length;

    /// <summary>
    /// Adds the samples, unless they've already been added. Returns the number of clips and collections added.
    /// </summary>
    public static async Task<(int Clips, int Collections)> SeedAsync(
        CuratorDbContext db,
        Profile caller,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Profiles.AnyAsync(p => p.Id == FrejaId, cancellationToken))
        {
            return (0, 0);
        }

        Guid[] people = [caller.Id, FrejaId, MadsId];
        db.Profiles.Add(new Profile(FrejaId, "Freja (sample)", now));
        db.Profiles.Add(new Profile(MadsId, "Mads (sample)", now));

        for (var i = 0; i < Clips.Length; i++)
        {
            var (title, description, recordedDaysAgo, uploader) = Clips[i];
            var recordedAt = recordedDaysAgo is { } days ? now.AddDays(-days).AddHours(-i) : (DateTimeOffset?)null;
            // Uploaded a little after recording, in the order of the list.
            var createdAt = (recordedAt ?? now.AddDays(-20)).AddHours(2);
            var asset = new Asset(people[uploader], $"{title}.mp4", "video/mp4", 10_000_000 + i, recordedAt, createdAt);
            asset.MarkUploaded(createdAt);
            db.Assets.Add(asset);
            // There's no way to set a description through the domain yet.
            db.Entry(asset).Property(a => a.Description).CurrentValue = description;
        }

        // An upload that never completed: not listed.
        db.Assets.Add(new Asset(caller.Id, "Abandoned upload.mp4", "video/mp4", 1_000, now, now));

        var collections = new Collection[Collections.Length];
        for (var i = 0; i < Collections.Length; i++)
        {
            var (name, description, parent, creator) = Collections[i];
            var parentId = parent is { } p ? collections[p].Id : (Guid?)null;
            collections[i] = new Collection(
                people[creator], parentId, name, description, now.AddDays(-40).AddHours(i));
            db.Collections.Add(collections[i]);
        }

        await db.SaveChangesAsync(cancellationToken);
        return (Clips.Length, Collections.Length);
    }

    /// <summary>
    /// Deletes every asset, including deleted and pending ones, every collection and the sample profiles. Real users'
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
        await db.Profiles.Where(p => p.Id == FrejaId || p.Id == MadsId).ExecuteDeleteAsync(cancellationToken);

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
