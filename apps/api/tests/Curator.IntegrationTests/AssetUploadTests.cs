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

    public static TheoryData<string, object> InvalidUploads => new()
    {
        { "Filename", new { filename = "", contentType = "video/mp4", sizeBytes = 1024 } },
        { "ContentType", new { filename = "photo.jpg", contentType = "image/jpeg", sizeBytes = 1024 } },
        { "SizeBytes", new { filename = "empty.mp4", contentType = "video/mp4", sizeBytes = 0 } },
        { "SizeBytes", new { filename = "huge.mp4", contentType = "video/mp4", sizeBytes = 6_000_000_000 } },
    };

    [Theory]
    [MemberData(nameof(InvalidUploads))]
    public async Task CreateUpload_WithInvalidFile_ReturnsValidationProblemAndCreatesNoAsset(string field, object body)
    {
        var userId = Guid.NewGuid();

        var response = await factory.CreateClient(userId).PostAsJsonAsync("/assets/uploads", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Contains(field, problem!.Errors.Keys);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.Assets.AnyAsync(a => a.UploadedBy == userId));
    }

    private sealed record UploadResponse(Guid AssetId, string UploadUrl, DateTimeOffset UploadUrlExpiresAt);

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
