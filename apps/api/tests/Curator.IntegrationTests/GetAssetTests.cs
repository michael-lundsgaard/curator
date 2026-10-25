using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class GetAssetTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Get_WhenUploaded_ReturnsAssetWithOriginalAsPlaybackAndNoThumbnail()
    {
        var (client, userId) = await CreateUserAsync();
        var recordedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var asset = await SaveAssetAsync(userId, recordedAt: recordedAt);

        var response = await client.GetAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var details = (await response.Content.ReadFromJsonAsync<Details>())!;
        Assert.Equal(asset.Id, details.Id);
        Assert.Equal("Funny moment", details.Title);
        Assert.Equal("Funny moment.mp4", details.OriginalFilename);
        Assert.Equal("video/mp4", details.ContentType);
        Assert.Equal(1024, details.SourceSizeBytes);
        Assert.Equal("Uploaded", details.Status);
        Assert.Equal(new Uploader(userId, "test.user"), details.UploadedBy);
        Assert.Equal(recordedAt, details.RecordedAt);
        AssertSignedUrl(asset.SourceKey!, details.PlaybackUrl);
        Assert.Null(details.ThumbnailUrl);
        Assert.InRange(details.UrlsExpireAt, DateTimeOffset.UtcNow.AddMinutes(59), DateTimeOffset.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task Get_WhenProcessed_SignsPlaybackVersionAndThumbnail()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, processed: true);

        var details = (await client.GetFromJsonAsync<Details>($"/assets/{asset.Id}"))!;

        AssertSignedUrl($"assets/{asset.Id}/playback.mp4", details.PlaybackUrl);
        AssertSignedUrl($"assets/{asset.Id}/thumbnail.jpg", details.ThumbnailUrl);
    }

    [Fact]
    public async Task Get_ByAnotherUser_ReturnsSharedAsset()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId);
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenPending_IsVisibleToUploaderWithoutPlayback()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, uploaded: false);

        var details = (await client.GetFromJsonAsync<Details>($"/assets/{asset.Id}"))!;

        Assert.Equal("Pending", details.Status);
        Assert.Null(details.PlaybackUrl);
    }

    [Fact]
    public async Task Get_WhenPendingAndByAnotherUser_ReturnsNotFound()
    {
        var (_, uploaderId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(uploaderId, uploaded: false);
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenDeleted_ReturnsNotFound()
    {
        var (client, userId) = await CreateUserAsync();
        var asset = await SaveAssetAsync(userId, deleted: true);

        var response = await client.GetAsync($"/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForUnknownAsset_ReturnsNotFound()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync($"/assets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static void AssertSignedUrl(string key, string? url)
    {
        Assert.NotNull(url);
        var uri = new Uri(url);
        Assert.Equal($"{CuratorApiFactory.R2AccountId}.r2.cloudflarestorage.com", uri.Host);
        Assert.Equal($"/{CuratorApiFactory.R2Bucket}/{key}", uri.AbsolutePath);
        Assert.Contains("X-Amz-Signature=", uri.Query);
    }

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
        DateTimeOffset? recordedAt = null,
        bool uploaded = true,
        bool processed = false,
        bool deleted = false)
    {
        var asset = new Asset(userId, "Funny moment.mp4", "video/mp4", 1024, recordedAt, DateTimeOffset.UtcNow);
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
        if (processed)
        {
            // There's no processing yet to set these.
            var entry = db.Entry(asset);
            entry.Property(a => a.Status).CurrentValue = AssetStatus.Ready;
            entry.Property(a => a.PlaybackKey).CurrentValue = $"assets/{asset.Id}/playback.mp4";
            entry.Property(a => a.ThumbnailKey).CurrentValue = $"assets/{asset.Id}/thumbnail.jpg";
        }

        await db.SaveChangesAsync();
        return asset;
    }

    private sealed record Details(
        Guid Id,
        string Title,
        string OriginalFilename,
        string ContentType,
        long SourceSizeBytes,
        string Status,
        Uploader UploadedBy,
        DateTimeOffset? RecordedAt,
        string? PlaybackUrl,
        string? ThumbnailUrl,
        DateTimeOffset UrlsExpireAt);

    private sealed record Uploader(Guid Id, string DisplayName);
}
