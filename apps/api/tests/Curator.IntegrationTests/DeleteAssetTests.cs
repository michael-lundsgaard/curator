using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DeleteAssetTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Delete_ByUploader_SoftDeletesAndHidesIt()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        var response = await client.DeleteAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deleted = (await response.Content.ReadFromJsonAsync<Deleted>())!;
        Assert.Equal(asset.Id, deleted.Id);

        await using var db = factory.CreateDbContext();
        var saved = await db.Assets.IgnoreQueryFilters().SingleAsync(a => a.Id == asset.Id);
        // Postgres stores microseconds; the response has the clock's 100 ns ticks.
        Assert.Equal(deleted.DeletedAt, saved.DeletedAt!.Value, TimeSpan.FromMilliseconds(1));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/assets/{asset.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_ByAdmin_Succeeds()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId);
        var (client, adminId) = await CreateUserAsync();
        await using (var db = factory.CreateDbContext())
        {
            await db.Profiles.Where(p => p.Id == adminId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Role, ProfileRole.Admin));
        }

        var response = await client.DeleteAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ByAnotherMember_ReturnsForbiddenAndKeepsIt()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId);
        var (client, _) = await CreateUserAsync();

        var response = await client.DeleteAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await using var db = factory.CreateDbContext();
        Assert.True(await db.Assets.AnyAsync(a => a.Id == asset.Id));
    }

    [Fact]
    public async Task Delete_WhenPending_IsAllowedForUploaderAndNotFoundForOthers()
    {
        var (uploader, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId, uploaded: false);
        var (other, _) = await CreateUserAsync();

        var byOther = await other.DeleteAsync($"/assets/{asset.Id}");
        var byUploader = await uploader.DeleteAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.NotFound, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byUploader.StatusCode);
    }

    [Fact]
    public async Task Delete_Twice_ReturnsNotFound()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        (await client.DeleteAsync($"/assets/{asset.Id}")).EnsureSuccessStatusCode();
        var response = await client.DeleteAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownAsset_ReturnsNotFound()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.DeleteAsync($"/assets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that assets reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return (client, userId);
    }

    private async Task<Asset> SaveAssetAsync(Guid userId, bool uploaded = true)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var asset = new Asset(userId, "Funny moment.mp4", "video/mp4", 1024, null, createdAt);
        if (uploaded)
        {
            asset.MarkUploaded(createdAt);
        }

        await using var db = factory.CreateDbContext();
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        return asset;
    }

    private sealed record Deleted(Guid Id, DateTimeOffset DeletedAt);
}
