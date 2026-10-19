using Curator.Domain.Assets;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;

namespace Curator.Api.Development;

/// <summary>
/// Sample people, clips and collections for trying the API locally, built as domain objects without touching the
/// database (<see cref="DevelopmentData.SeedAsync"/> saves them). The clips have no files in storage, so nothing is
/// uploaded to R2 and nothing is left behind there.
/// </summary>
public sealed class SampleData
{
    internal static readonly Guid FrejaId = new("0199f0a0-0000-7000-8000-000000000001");
    internal static readonly Guid MadsId = new("0199f0a0-0000-7000-8000-000000000002");

    private readonly DateTimeOffset now;
    private readonly List<Profile> people = [];
    private readonly List<Collection> collections = [];
    private readonly List<Asset> clips = [];
    private readonly List<Asset> abandonedUploads = [];
    private readonly List<CollectionAsset> collectionClips = [];

    /// <param name="callerId">The person asking for samples. Some of the samples are theirs.</param>
    /// <param name="now">What the dates are relative to.</param>
    public SampleData(Guid callerId, DateTimeOffset now)
    {
        this.now = now;

        var me = callerId;
        var freja = Person(FrejaId, "Freja (sample)");
        var mads = Person(MadsId, "Mads (sample)");

        // A tree three levels deep, by different people. "hangouts" is lowercase on purpose: collections sort by name
        // ignoring case.
        var repo = Collection("R.E.P.O", by: mads, description: "Monster-filled evenings");
        var phasmophobia = Collection("Phasmophobia", by: me);
        var hauntedHouse = Collection(
            "Haunted house", by: mads, inside: phasmophobia, description: "Two nights in a row");
        var lethalCompany = Collection("Lethal Company", by: freja);
        var bestOf = Collection("Best of", by: freja, description: "The clips worth showing others");
        var bestOf2025 = Collection("2025", by: freja, inside: bestOf);
        var bestOf2026 = Collection("2026", by: me, inside: bestOf);
        var screams = Collection("Screams", by: mads, inside: bestOf2026);
        var fails = Collection("Fails", by: freja, inside: bestOf2026);
        var hangouts = Collection("hangouts", by: me);

        // Clips, newest first. One has no recording date, and some titles have Danish letters.
        var repoScream = Clip("R.E.P.O - the scream", by: mads, recordedDaysAgo: 1, "Mads finds the monster first");
        var truck = Clip("Funny moment in the truck", by: freja, recordedDaysAgo: 1);
        var jumpscare = Clip("Lethal Company: jumpscare", by: me, recordedDaysAgo: 3, "Coil-head behind the door");
        var beers = Clip("Øl hos Søren", by: freja, recordedDaysAgo: 4, "Skål!");
        var ghostHunt = Clip("Phasmophobia ghost hunt", by: mads, recordedDaysAgo: 6, "Night two in the haunted house");
        var ghostPhoto = Clip("Phasmophobia: the ghost photo", by: me, recordedDaysAgo: 6);
        var bestOfOctober = Clip("Best of October", by: freja, recordedDaysAgo: 9, "Compilation");
        Clip("Speedrun attempt 3", by: mads, recordedDaysAgo: 12);
        Clip("100% completion!", by: me, recordedDaysAgo: 14, "Finally");
        Clip("Content Warning: we went viral", by: freja, recordedDaysAgo: 15);
        Clip("Ærlig talt, hvad skete der", by: mads, recordedDaysAgo: 18);
        var bossFail = Clip("Boss fight fail", by: freja, recordedDaysAgo: 21, "So close");
        Clip("Boss fight win", by: mads, recordedDaysAgo: 21, "Third time lucky");
        Clip("Dancing NPC", by: freja, recordedDaysAgo: null);
        var lagSpike = Clip("Lag spike of the century", by: me, recordedDaysAgo: 30);
        var hideAndSeek = Clip("Hide and seek", by: mads, recordedDaysAgo: 34, "Nobody found Freja");
        var snowFight = Clip("Winter holiday 2025: snow fight", by: freja, recordedDaysAgo: 300);
        Clip("Old clip from the archive", by: mads, recordedDaysAgo: 700, "Recorded before Curator existed");

        // Some clips are in several collections, some in none. "2026" only has sub-collections, so it counts no clips
        // of its own.
        AddClips(repo, by: mads, repoScream);
        AddClips(phasmophobia, by: me, ghostHunt, ghostPhoto);
        AddClips(hauntedHouse, by: mads, ghostHunt);
        AddClips(lethalCompany, by: freja, truck, jumpscare);
        AddClips(bestOf, by: freja, bestOfOctober);
        AddClips(bestOf2025, by: freja, snowFight);
        AddClips(screams, by: mads, repoScream, jumpscare);
        AddClips(fails, by: freja, bossFail, lagSpike);
        AddClips(hangouts, by: me, beers, hideAndSeek);

        AbandonedUpload("Abandoned upload", by: me);
    }

    /// <summary>The sample people. The caller isn't one of them: their profile exists already.</summary>
    public IReadOnlyList<Profile> People => people;

    /// <summary>Parents come before their sub-collections.</summary>
    public IReadOnlyList<Collection> Collections => collections;

    /// <summary>Uploaded clips, all listed in the library.</summary>
    public IReadOnlyList<Asset> Clips => clips;

    /// <summary>Uploads that never completed, so they aren't listed.</summary>
    public IReadOnlyList<Asset> AbandonedUploads => abandonedUploads;

    /// <summary>Which clips are in which collections.</summary>
    public IReadOnlyList<CollectionAsset> CollectionClips => collectionClips;

    private Guid Person(Guid id, string displayName)
    {
        people.Add(new Profile(id, displayName, now));
        return id;
    }

    private Collection Collection(string name, Guid by, Collection? inside = null, string? description = null)
    {
        // Created before the clips, in the order listed.
        var createdAt = now.AddDays(-40).AddHours(collections.Count);
        var collection = new Collection(by, inside?.Id, name, description, createdAt);
        collections.Add(collection);
        return collection;
    }

    private Asset Clip(string title, Guid by, int? recordedDaysAgo, string? description = null)
    {
        // An hour apart, so clips recorded the same day still have an order.
        var recordedAt = recordedDaysAgo is { } days
            ? now.AddDays(-days).AddHours(-clips.Count)
            : (DateTimeOffset?)null;
        // Uploaded a little after recording.
        var createdAt = (recordedAt ?? now.AddDays(-20)).AddHours(2);

        var clip = new Asset(by, $"{title}.mp4", "video/mp4", 10_000_000 + clips.Count, recordedAt, createdAt);
        clip.MarkUploaded(createdAt);
        clip.Edit(title, description, recordedAt, createdAt);
        clips.Add(clip);
        return clip;
    }

    private void AddClips(Collection collection, Guid by, params Asset[] clipsToAdd)
    {
        foreach (var clip in clipsToAdd)
        {
            // Added a little after uploading.
            collectionClips.Add(new CollectionAsset(collection.Id, clip.Id, by, clip.CreatedAt.AddHours(1)));
        }
    }

    private void AbandonedUpload(string title, Guid by)
    {
        abandonedUploads.Add(new Asset(by, $"{title}.mp4", "video/mp4", 1_000, now, now));
    }
}
