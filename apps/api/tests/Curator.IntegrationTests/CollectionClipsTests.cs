using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

/// <summary>Adding clips to collections and removing them, which everyone may do in every collection.</summary>
[Collection(ApiCollection.Name)]
public class CollectionClipsTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Add_ToAnotherUsersCollection_AddsClipsAndSkipsThoseAlreadyIn()
    {
        var (owner, ownerId) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(owner, "Screams");
        var (client, userId) = await CreateUserAsync();
        var first = await SaveAssetAsync(ownerId);
        var second = await SaveAssetAsync(userId);

        var response = await client.PostAsJsonAsync(
            $"/collections/{collection}/assets", new { assetIds = new[] { first.Id, first.Id } });
        var again = await client.PostAsJsonAsync(
            $"/collections/{collection}/assets", new { assetIds = new[] { first.Id, second.Id } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Added(collection, 1), await response.Content.ReadFromJsonAsync<Added>());
        Assert.Equal(new Added(collection, 1), await again.Content.ReadFromJsonAsync<Added>());
        await using var db = factory.CreateDbContext();
        var rows = await db.CollectionAssets.Where(ca => ca.CollectionId == collection).ToListAsync();
        Assert.Equal([first.Id, second.Id], rows.Select(r => r.AssetId).Order());
        Assert.All(rows, r => Assert.Equal(userId, r.AddedBy));
    }

    [Fact]
    public async Task Add_WithDeletedOrPendingClip_ReturnsBadRequest_AndAddsNothing()
    {
        var (client, userId) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(client, "Screams");
        var uploaded = await SaveAssetAsync(userId);
        var pending = await SaveAssetAsync(userId, uploaded: false);
        var deleted = await SaveAssetAsync(userId, deleted: true);

        foreach (var asset in new[] { pending, deleted })
        {
            var response = await client.PostAsJsonAsync(
                $"/collections/{collection}/assets", new { assetIds = new[] { uploaded.Id, asset.Id } });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        await using var db = factory.CreateDbContext();
        Assert.False(await db.CollectionAssets.AnyAsync(ca => ca.CollectionId == collection));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Add_WithTooFewOrTooManyClips_ReturnsBadRequest(int count)
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(client, "Screams");
        var assetIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid());

        var response = await client.PostAsJsonAsync($"/collections/{collection}/assets", new { assetIds });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_ToUnknownCollection_ReturnsNotFound()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        var response = await client.PostAsJsonAsync(
            $"/collections/{Guid.NewGuid()}/assets", new { assetIds = new[] { asset.Id } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_ByAnotherUser_TakesClipsOut_SkipsOthers_AndKeepsThemInTheLibrary()
    {
        var (owner, ownerId) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(owner, "Screams");
        var first = await SaveAssetAsync(ownerId);
        var second = await SaveAssetAsync(ownerId);
        var kept = await SaveAssetAsync(ownerId);
        await AddAsync(owner, collection, first.Id, second.Id, kept.Id);
        var (client, _) = await CreateUserAsync();
        var assetIds = new[] { first.Id, second.Id, Guid.NewGuid() };

        var response = await client.PostAsJsonAsync($"/collections/{collection}/assets/remove", new { assetIds });
        var again = await client.PostAsJsonAsync($"/collections/{collection}/assets/remove", new { assetIds });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Removed(collection, 2), await response.Content.ReadFromJsonAsync<Removed>());
        Assert.Equal(new Removed(collection, 0), await again.Content.ReadFromJsonAsync<Removed>());
        await using var db = factory.CreateDbContext();
        Assert.Equal(
            [kept.Id],
            await db.CollectionAssets
                .Where(ca => ca.CollectionId == collection)
                .Select(ca => ca.AssetId)
                .ToListAsync());
        Assert.Equal(3, await db.Assets.CountAsync(a => a.Id == first.Id || a.Id == second.Id || a.Id == kept.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Remove_WithTooFewOrTooManyClips_ReturnsBadRequest(int count)
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(client, "Screams");
        var assetIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid());

        var response = await client.PostAsJsonAsync($"/collections/{collection}/assets/remove", new { assetIds });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_FromUnknownCollection_ReturnsNotFound()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync(
            $"/collections/{Guid.NewGuid()}/assets/remove", new { assetIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCollection_RemovesItsClips_ButKeepsThemInTheLibrary()
    {
        var (client, userId) = await CreateUserAsync();
        var collection = await CreateCollectionAsync(client, "Screams");
        var child = await CreateCollectionAsync(client, "Loud", collection);
        var asset = await SaveAssetAsync(userId);
        await AddAsync(client, collection, asset.Id);
        await AddAsync(client, child, asset.Id);

        (await client.DeleteAsync($"/collections/{collection}")).EnsureSuccessStatusCode();

        await using var db = factory.CreateDbContext();
        Assert.False(await db.CollectionAssets.AnyAsync(ca => ca.AssetId == asset.Id));
        Assert.True(await db.Assets.AnyAsync(a => a.Id == asset.Id));
    }

    [Fact]
    public async Task List_CountsEachCollectionsOwnClips_WithoutDeletedOnes()
    {
        var (client, userId) = await CreateUserAsync();
        var parent = await CreateCollectionAsync(client, "Screams");
        var child = await CreateCollectionAsync(client, "Loud", parent);
        var empty = await CreateCollectionAsync(client, "Quiet");
        var kept = await SaveAssetAsync(userId);
        var other = await SaveAssetAsync(userId);
        var toDelete = await SaveAssetAsync(userId);
        await AddAsync(client, parent, kept.Id, other.Id, toDelete.Id);
        await AddAsync(client, child, kept.Id);
        (await client.DeleteAsync($"/assets/{toDelete.Id}")).EnsureSuccessStatusCode();

        var items = (await client.GetFromJsonAsync<Page>("/collections"))!.Items
            .ToDictionary(i => i.Id, i => i.ClipCount);

        Assert.Equal(2, items[parent]);
        Assert.Equal(1, items[child]);
        Assert.Equal(0, items[empty]);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that collections and assets reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return (client, userId);
    }

    private async Task<Asset> SaveAssetAsync(Guid userId, bool uploaded = true, bool deleted = false)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var asset = new Asset(userId, "Funny moment.mp4", "video/mp4", 1024, null, createdAt);
        if (uploaded)
        {
            asset.MarkUploaded(createdAt);
        }

        if (deleted)
        {
            asset.Delete(createdAt);
        }

        await using var db = factory.CreateDbContext();
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        return asset;
    }

    private static async Task<Guid> CreateCollectionAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var response = await client.PostAsJsonAsync("/collections", new { name, parentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Item>())!.Id;
    }

    private static async Task AddAsync(HttpClient client, Guid collectionId, params Guid[] assetIds)
    {
        var response = await client.PostAsJsonAsync($"/collections/{collectionId}/assets", new { assetIds });
        response.EnsureSuccessStatusCode();
    }

    private sealed record Added(Guid Id, int AddedCount);

    private sealed record Removed(Guid Id, int RemovedCount);

    private sealed record Page(IReadOnlyList<Item> Items);

    private sealed record Item(Guid Id, int ClipCount);
}
