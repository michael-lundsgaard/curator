using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class EditAssetTests(CuratorApiFactory factory)
{
    private static readonly DateTimeOffset RecordedAt = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Edit_ByUploader_SavesDetails()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        var response = await client.PatchAsJsonAsync(
            $"/assets/{asset.Id}", new { title = " Screams ", description = "The best one", recordedAt = RecordedAt });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = (await response.Content.ReadFromJsonAsync<Edited>())!;
        Assert.Equal(new Edited(asset.Id, "Screams", "The best one", RecordedAt), edited);

        await using var db = factory.CreateDbContext();
        var saved = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        Assert.Equal("Screams", saved.Title);
        Assert.Equal("The best one", saved.Description);
        Assert.Equal(RecordedAt, saved.RecordedAt);
        Assert.True(saved.UpdatedAt > asset.UpdatedAt);
    }

    [Fact]
    public async Task Edit_WithNullDescriptionAndRecordedAt_ClearsThem()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, description: "Something", recordedAt: RecordedAt);

        var response = await client.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = (await response.Content.ReadFromJsonAsync<Edited>())!;
        Assert.Null(edited.Description);
        Assert.Null(edited.RecordedAt);
    }

    // The body replaces all three, so a field left out by mistake would otherwise clear it.
    [Fact]
    public async Task Edit_WithoutDescription_ReturnsBadRequest_AndKeepsIt()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, description: "Something", recordedAt: RecordedAt);

        var response = await client.PatchAsJsonAsync(
            $"/assets/{asset.Id}", new { title = "Screams", recordedAt = RecordedAt });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("description", await response.Content.ReadAsStringAsync());
        await using var db = factory.CreateDbContext();
        Assert.Equal("Something", (await db.Assets.SingleAsync(a => a.Id == asset.Id)).Description);
    }

    [Fact]
    public async Task Edit_ByAdmin_SavesDetails()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId);
        var (client, adminId) = await CreateUserAsync();
        await using (var db = factory.CreateDbContext())
        {
            await db.Profiles.Where(p => p.Id == adminId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Role, ProfileRole.Admin));
        }

        var response = await client.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_ByAnotherMember_ReturnsForbidden()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId);
        var (client, _) = await CreateUserAsync();

        var response = await client.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Edit_WhenPending_IsAllowedForUploaderAndNotFoundForOthers()
    {
        var (uploader, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId, uploaded: false);
        var (other, _) = await CreateUserAsync();

        var byUploader = await uploader.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));
        var byOther = await other.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));

        Assert.Equal(HttpStatusCode.OK, byUploader.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, byOther.StatusCode);
    }

    [Fact]
    public async Task Edit_WhenDeleted_ReturnsNotFound()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, deleted: true);

        var response = await client.PatchAsJsonAsync($"/assets/{asset.Id}", Body("Screams"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Edit_WithoutTitle_ReturnsBadRequest(string? title)
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        var response = await client.PatchAsJsonAsync($"/assets/{asset.Id}", Body(title));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WithTooLongDescription_ReturnsBadRequest()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId);

        var response = await client.PatchAsJsonAsync(
            $"/assets/{asset.Id}",
            new { title = "Screams", description = new string('a', 5001), recordedAt = (DateTimeOffset?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Every field must be sent; null clears the description and recording time.
    private static object Body(string? title) =>
        new { title, description = (string?)null, recordedAt = (DateTimeOffset?)null };

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that assets reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return (client, userId);
    }

    private async Task<Asset> SaveAssetAsync(
        Guid userId,
        string? description = null,
        DateTimeOffset? recordedAt = null,
        bool uploaded = true,
        bool deleted = false)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var asset = new Asset(userId, "Funny moment.mp4", "video/mp4", 1024, recordedAt, createdAt);
        if (description is not null)
        {
            asset.Edit(asset.Title, description, recordedAt, createdAt);
        }

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

    private sealed record Edited(Guid Id, string Title, string? Description, DateTimeOffset? RecordedAt);
}
