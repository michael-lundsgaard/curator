using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Assets;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AssetUploadTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task CreateUpload_CreatesPendingAssetAndReturnsUploadUrl()
    {
        var userId = Guid.NewGuid();
        // Browsers send local times. Postgres only accepts UTC, so this must be converted, not rejected.
        var recordedAt = new DateTimeOffset(2026, 10, 16, 20, 15, 0, TimeSpan.FromHours(2));

        var response = await factory.CreateClient(userId).PostAsJsonAsync("/assets/uploads",
            new { filename = "Funny moment.mp4", contentType = "video/mp4", sizeBytes = 1024, recordedAt });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var upload = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.NotNull(upload);

        await using var db = factory.CreateDbContext();
        var asset = await db.Assets.SingleAsync(a => a.Id == upload.AssetId);
        Assert.Equal(userId, asset.UploadedBy);
        Assert.Equal("Funny moment", asset.Title);
        Assert.Equal("Funny moment.mp4", asset.OriginalFilename);
        Assert.Equal("video/mp4", asset.ContentType);
        Assert.Equal(AssetStatus.Pending, asset.Status);
        Assert.Equal(1024, asset.SourceSizeBytes);
        Assert.Equal(recordedAt, asset.RecordedAt);

        var url = new Uri(upload.UploadUrl);
        Assert.Equal($"{CuratorApiFactory.R2AccountId}.r2.cloudflarestorage.com", url.Host);
        Assert.Equal($"/{CuratorApiFactory.R2Bucket}/{asset.SourceKey}", url.AbsolutePath);
        Assert.Contains("X-Amz-Signature=", url.Query);
        Assert.InRange(upload.UploadUrlExpiresAt, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(16));
    }

    // Plain values rather than objects, so xUnit can serialize each case and test explorers list them separately.
    [Theory]
    [InlineData("Filename", "", "video/mp4", 1024L)]
    [InlineData("ContentType", "photo.jpg", "image/jpeg", 1024L)]
    [InlineData("SizeBytes", "empty.mp4", "video/mp4", 0L)]
    [InlineData("SizeBytes", "huge.mp4", "video/mp4", 6_000_000_000L)]
    public async Task CreateUpload_WithInvalidFile_ReturnsValidationProblemAndCreatesNoAsset(
        string field,
        string filename,
        string contentType,
        long sizeBytes)
    {
        var userId = Guid.NewGuid();

        var response = await factory.CreateClient(userId).PostAsJsonAsync("/assets/uploads",
            new { filename, contentType, sizeBytes });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Contains(field, problem!.Errors.Keys);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.Assets.AnyAsync(a => a.UploadedBy == userId));
    }

    [Fact]
    public async Task CompleteUpload_WhenFileIsStored_MarksAssetUploaded()
    {
        var client = factory.CreateClient(Guid.NewGuid());
        var asset = await StartUploadAsync(client, sizeBytes: 1024);
        factory.StoredObjects[asset.SourceKey!] = 1024;

        var response = await client.PostAsync($"/assets/{asset.Id}/complete-upload", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var completed = await response.Content.ReadFromJsonAsync<CompleteResponse>();
        Assert.Equal(new CompleteResponse(asset.Id, "Uploaded"), completed);
        Assert.Equal(AssetStatus.Uploaded, await GetStatusAsync(asset.Id));
    }

    [Fact]
    public async Task CompleteUpload_WhenAlreadyCompleted_ReturnsCurrentStatus()
    {
        var client = factory.CreateClient(Guid.NewGuid());
        var asset = await StartUploadAsync(client, sizeBytes: 1024);
        factory.StoredObjects[asset.SourceKey!] = 1024;
        await client.PostAsync($"/assets/{asset.Id}/complete-upload", null);

        var response = await client.PostAsync($"/assets/{asset.Id}/complete-upload", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var completed = await response.Content.ReadFromJsonAsync<CompleteResponse>();
        Assert.Equal("Uploaded", completed!.Status);
    }

    [Theory]
    [InlineData(null)] // Not uploaded yet.
    [InlineData(2048L)] // Bigger than announced.
    public async Task CompleteUpload_WhenFileIsMissingOrHasWrongSize_ReturnsConflictAndStaysPending(long? storedSize)
    {
        var client = factory.CreateClient(Guid.NewGuid());
        var asset = await StartUploadAsync(client, sizeBytes: 1024);
        if (storedSize is not null)
        {
            factory.StoredObjects[asset.SourceKey!] = storedSize.Value;
        }

        var response = await client.PostAsync($"/assets/{asset.Id}/complete-upload", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(AssetStatus.Pending, await GetStatusAsync(asset.Id));
    }

    [Fact]
    public async Task CompleteUpload_ByAnotherUser_ReturnsForbidden()
    {
        var asset = await StartUploadAsync(factory.CreateClient(Guid.NewGuid()), sizeBytes: 1024);
        factory.StoredObjects[asset.SourceKey!] = 1024;

        var response = await factory.CreateClient(Guid.NewGuid())
            .PostAsync($"/assets/{asset.Id}/complete-upload", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(AssetStatus.Pending, await GetStatusAsync(asset.Id));
    }

    [Fact]
    public async Task CompleteUpload_ForUnknownAsset_ReturnsNotFound()
    {
        var response = await factory.CreateClient(Guid.NewGuid())
            .PostAsync($"/assets/{Guid.NewGuid()}/complete-upload", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Asset> StartUploadAsync(HttpClient client, long sizeBytes)
    {
        var response = await client.PostAsJsonAsync("/assets/uploads",
            new { filename = "clip.mp4", contentType = "video/mp4", sizeBytes });
        response.EnsureSuccessStatusCode();
        var upload = await response.Content.ReadFromJsonAsync<UploadResponse>();

        await using var db = factory.CreateDbContext();
        return await db.Assets.SingleAsync(a => a.Id == upload!.AssetId);
    }

    private async Task<AssetStatus> GetStatusAsync(Guid assetId)
    {
        await using var db = factory.CreateDbContext();
        return await db.Assets.Where(a => a.Id == assetId).Select(a => a.Status).SingleAsync();
    }

    private sealed record UploadResponse(Guid AssetId, string UploadUrl, DateTimeOffset UploadUrlExpiresAt);

    private sealed record CompleteResponse(Guid AssetId, string Status);

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
