using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.Infrastructure.Persistence;

/// <summary>
/// Sample profiles and clips for trying the API locally (Development only). The clips have no files in storage,
/// so nothing is uploaded to R2 and nothing is left behind there.
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

    /// <summary>Adds the samples, unless they've already been added. Returns the number of clips added.</summary>
    public static async Task<int> SeedAsync(
        CuratorDbContext db,
        Profile caller,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Profiles.AnyAsync(p => p.Id == FrejaId, cancellationToken))
        {
            return 0;
        }

        Guid[] uploaders = [caller.Id, FrejaId, MadsId];
        db.Profiles.Add(new Profile(FrejaId, "Freja (sample)", now));
        db.Profiles.Add(new Profile(MadsId, "Mads (sample)", now));

        for (var i = 0; i < Clips.Length; i++)
        {
            var (title, description, recordedDaysAgo, uploader) = Clips[i];
            var recordedAt = recordedDaysAgo is { } days ? now.AddDays(-days).AddHours(-i) : (DateTimeOffset?)null;
            // Uploaded a little after recording, in the order of the list.
            var createdAt = (recordedAt ?? now.AddDays(-20)).AddHours(2);
            var asset = new Asset(uploaders[uploader], $"{title}.mp4", "video/mp4", 10_000_000 + i, recordedAt, createdAt);
            asset.MarkUploaded(createdAt);
            db.Assets.Add(asset);
            // There's no way to set a description through the domain yet.
            db.Entry(asset).Property(a => a.Description).CurrentValue = description;
        }

        // An upload that never completed: not listed.
        db.Assets.Add(new Asset(caller.Id, "Abandoned upload.mp4", "video/mp4", 1_000, now, now));

        await db.SaveChangesAsync(cancellationToken);
        return Clips.Length;
    }
}
