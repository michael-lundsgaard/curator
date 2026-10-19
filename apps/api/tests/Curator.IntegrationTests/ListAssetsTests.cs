using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;
using Microsoft.AspNetCore.WebUtilities;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ListAssetsTests(CuratorApiFactory factory)
{
    private static readonly DateTimeOffset Day = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_ReturnsUploadedAssetsRecordedNewestFirst_AndHidesPendingAndDeleted()
    {
        var (client, userId) = await CreateUserAsync();
        var older = await SaveAssetAsync(userId, "Older", recordedAt: Day);
        var newer = await SaveAssetAsync(userId, "Newer", recordedAt: Day.AddDays(1));
        await SaveAssetAsync(userId, "Pending", recordedAt: Day, uploaded: false);
        await SaveAssetAsync(userId, "Deleted", recordedAt: Day, deleted: true);

        var page = await ListAsync(client, new() { ["uploadedBy"] = userId.ToString() });

        Assert.Equal([newer, older], page.Items.Select(i => i.Id));
        Assert.Null(page.NextCursor);
        var item = page.Items[0];
        Assert.Equal("Newer", item.Title);
        Assert.Equal("Uploaded", item.Status);
        Assert.Equal(new Uploader(userId, "test.user"), item.UploadedBy);
        Assert.Equal(Day.AddDays(1), item.RecordedAt);
    }

    // Every sort and order, paged two at a time. The assets include ties on the key ("B" and "b" have the same
    // recorded_at and the same title ignoring case), broken by the ID in the same direction, and "c" has no
    // recorded_at, so it sorts by when it was uploaded.
    [Theory]
    [InlineData("recorded", null, "a,b,B,c,d")]
    [InlineData("recorded", "asc", "d,c,B,b,a")]
    [InlineData("uploaded", null, "d,c,b,B,a")]
    [InlineData("uploaded", "asc", "a,B,b,c,d")]
    [InlineData("title", null, "a,B,b,c,d")]
    [InlineData("title", "desc", "d,c,b,B,a")]
    public async Task List_PagesThroughEverySortWithoutGapsOrDuplicates(string sort, string? order, string expected)
    {
        var (client, userId) = await CreateUserAsync();
        // Uploaded in this order; IDs (the tie-breaker) increase with it.
        await SaveAssetAsync(userId, "a", recordedAt: Day.AddDays(5), createdAt: Day);
        await SaveAssetAsync(userId, "B", recordedAt: Day.AddDays(3), createdAt: Day.AddMinutes(1));
        await SaveAssetAsync(userId, "b", recordedAt: Day.AddDays(3), createdAt: Day.AddMinutes(2));
        await SaveAssetAsync(userId, "c", recordedAt: null, createdAt: Day.AddDays(2));
        await SaveAssetAsync(userId, "d", recordedAt: Day.AddDays(1), createdAt: Day.AddDays(4));

        var parameters = new Dictionary<string, string?>
        {
            ["uploadedBy"] = userId.ToString(),
            ["sort"] = sort,
            ["order"] = order,
            ["limit"] = "2",
        };
        var titles = await ListAllTitlesAsync(client, parameters);

        Assert.Equal(expected, string.Join(",", titles));
    }

    [Fact]
    public async Task List_WhenAssetIsAddedWhileScrolling_ContinuesWithoutDuplicates()
    {
        var (client, userId) = await CreateUserAsync();
        for (var i = 1; i <= 4; i++)
        {
            await SaveAssetAsync(userId, $"Clip {i}", recordedAt: Day.AddDays(i));
        }

        var parameters = new Dictionary<string, string?> { ["uploadedBy"] = userId.ToString(), ["limit"] = "2" };
        var first = await ListAsync(client, parameters);
        // A new clip at the top would shift every offset-based page by one.
        await SaveAssetAsync(userId, "Clip 5", recordedAt: Day.AddDays(5));
        var second = await ListAsync(client, new(parameters) { ["cursor"] = first.NextCursor });

        Assert.Equal(["Clip 4", "Clip 3"], first.Items.Select(i => i.Title));
        Assert.Equal(["Clip 2", "Clip 1"], second.Items.Select(i => i.Title));
    }

    [Theory]
    [InlineData("funny scream", "Funny moment: the scream")] // Every word, in any order.
    [InlineData("SCRE", "Funny moment: the scream")] // Part of a word, any case.
    [InlineData("haunted", "Funny moment: the scream")] // In the description.
    [InlineData("øl", "Øl hos Søren")] // Danish letters, lowercased by Postgres and .NET alike.
    [InlineData("SØREN", "Øl hos Søren")]
    [InlineData("100%", "100% scary")] // LIKE wildcards are matched literally.
    public async Task List_WithSearch_ReturnsAssetsContainingEveryWord(string q, string expected)
    {
        var (client, userId) = await CreateUserAsync();
        await SaveAssetAsync(userId, "Funny moment: the scream", description: "Night two in the haunted house");
        await SaveAssetAsync(userId, "Funny moment");
        await SaveAssetAsync(userId, "Øl hos Søren");
        await SaveAssetAsync(userId, "100% scary");
        await SaveAssetAsync(userId, "1000 scary");

        var page = await ListAsync(client, new() { ["uploadedBy"] = userId.ToString(), ["q"] = q });

        Assert.Equal([expected], page.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task List_WithRecordedRange_IncludesFromAndExcludesTo()
    {
        var (client, userId) = await CreateUserAsync();
        await SaveAssetAsync(userId, "Before", recordedAt: Day.AddDays(-1));
        await SaveAssetAsync(userId, "From", recordedAt: Day);
        await SaveAssetAsync(userId, "To", recordedAt: Day.AddDays(1));

        var page = await ListAsync(client, new()
        {
            ["uploadedBy"] = userId.ToString(),
            ["recordedFrom"] = Day.ToString("O"),
            ["recordedTo"] = Day.AddDays(1).ToString("O"),
        });

        Assert.Equal(["From"], page.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task List_WithCollectionId_ReturnsItsClips_AndWithSubCollectionsTheirsToo_EachOnce()
    {
        var (client, userId) = await CreateUserAsync();
        var root = await CreateCollectionAsync(client, "Horror");
        var child = await CreateCollectionAsync(client, "Phasmophobia", root);
        var grandchild = await CreateCollectionAsync(client, "Screams", child);
        var other = await CreateCollectionAsync(client, "Racing");
        var a = await SaveAssetAsync(userId, "a");
        var b = await SaveAssetAsync(userId, "b");
        var c = await SaveAssetAsync(userId, "c");
        var d = await SaveAssetAsync(userId, "d");
        await SaveAssetAsync(userId, "Not in any");
        await AddClipsAsync(client, root, a);
        await AddClipsAsync(client, child, a, b); // a is in two of them.
        await AddClipsAsync(client, grandchild, c);
        await AddClipsAsync(client, other, d);

        var own = await ListAllTitlesAsync(client, new() { ["collectionId"] = root.ToString() });
        var all = await ListAllTitlesAsync(client, new()
        {
            ["collectionId"] = root.ToString(),
            ["includeSubCollections"] = "true",
            ["sort"] = "title",
            ["limit"] = "2",
        });

        Assert.Equal(["a"], own);
        Assert.Equal(["a", "b", "c"], all);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task List_WithUnknownCollectionId_ReturnsNotFound(bool includeSubCollections)
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync(Url(new()
        {
            ["collectionId"] = Guid.NewGuid().ToString(),
            ["includeSubCollections"] = includeSubCollections.ToString(),
        }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_WithCursorFromAnotherQuery_ReturnsBadRequest()
    {
        var (client, userId) = await CreateUserAsync();
        await SaveAssetAsync(userId, "One");
        await SaveAssetAsync(userId, "Two");
        var first = await ListAsync(client, new() { ["uploadedBy"] = userId.ToString(), ["limit"] = "1" });

        var response = await client.GetAsync(Url(new()
        {
            ["uploadedBy"] = userId.ToString(),
            ["limit"] = "1",
            ["sort"] = "title",
            ["cursor"] = first.NextCursor,
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("cursor", "not-a-cursor")]
    [InlineData("limit", "101")]
    [InlineData("sort", "size")]
    [InlineData("includeSubCollections", "true")] // Without a collectionId.
    public async Task List_WithInvalidParameter_ReturnsBadRequest(string name, string value)
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync(Url(new() { [name] = value }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that assets reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return (client, userId);
    }

    private async Task<Guid> SaveAssetAsync(
        Guid userId,
        string title,
        string? description = null,
        DateTimeOffset? recordedAt = null,
        DateTimeOffset? createdAt = null,
        bool uploaded = true,
        bool deleted = false)
    {
        var asset = new Asset(userId, $"{title}.mp4", "video/mp4", 1024, recordedAt, createdAt ?? DateTimeOffset.UtcNow);
        if (uploaded)
        {
            asset.MarkUploaded(asset.CreatedAt);
        }

        if (deleted)
        {
            asset.Delete(asset.CreatedAt);
        }

        await using var db = factory.CreateDbContext();
        db.Assets.Add(asset);
        // There's no way to set a description through the domain yet.
        db.Entry(asset).Property(a => a.Description).CurrentValue = description;
        await db.SaveChangesAsync();
        return asset.Id;
    }

    private static async Task<Guid> CreateCollectionAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var response = await client.PostAsJsonAsync("/collections", new { name, parentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!.Id;
    }

    private static async Task AddClipsAsync(HttpClient client, Guid collectionId, params Guid[] assetIds)
    {
        var response = await client.PostAsJsonAsync($"/collections/{collectionId}/assets", new { assetIds });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Page> ListAsync(HttpClient client, Dictionary<string, string?> parameters)
    {
        var response = await client.GetAsync(Url(parameters));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Page>())!;
    }

    private static async Task<List<string>> ListAllTitlesAsync(
        HttpClient client,
        Dictionary<string, string?> parameters)
    {
        var titles = new List<string>();
        string? cursor = null;
        do
        {
            var page = await ListAsync(client, new(parameters) { ["cursor"] = cursor });
            titles.AddRange(page.Items.Select(i => i.Title));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        return titles;
    }

    private static string Url(Dictionary<string, string?> parameters) =>
        QueryHelpers.AddQueryString("/assets", parameters.Where(p => p.Value is not null));

    private sealed record Page(List<Item> Items, string? NextCursor);

    private sealed record Item(
        Guid Id,
        string Title,
        string? Description,
        string Status,
        Uploader UploadedBy,
        DateTimeOffset? RecordedAt,
        DateTimeOffset CreatedAt);

    private sealed record Uploader(Guid Id, string DisplayName);

    private sealed record Created(Guid Id);
}
