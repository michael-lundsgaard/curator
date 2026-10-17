using Curator.Domain.Assets;

namespace Curator.UnitTests.Assets;

public class AssetTests
{
    private static Asset CreateAsset(string filename = "clip.mp4") =>
        new(Guid.NewGuid(), filename, "video/mp4", 1024, null, DateTimeOffset.UtcNow);

    [Fact]
    public void New_IsPendingWithKeyDerivedFromId()
    {
        var asset = CreateAsset("../../Funny moment.mp4");

        Assert.Equal(AssetStatus.Pending, asset.Status);
        Assert.Equal($"assets/{asset.Id}/original", asset.SourceKey);
    }

    [Theory]
    [InlineData("Funny moment.mp4", "Funny moment")]
    [InlineData("clip.final.mkv", "clip.final")]
    [InlineData("no-extension", "no-extension")]
    [InlineData(".mp4", ".mp4")]
    public void New_TitleDefaultsToFilenameWithoutExtension(string filename, string title)
    {
        Assert.Equal(title, CreateAsset(filename).Title);
    }

    [Fact]
    public void MarkUploaded_WhenPending_IsUploaded()
    {
        var asset = CreateAsset();
        var now = asset.CreatedAt.AddMinutes(1);

        asset.MarkUploaded(now);

        Assert.Equal(AssetStatus.Uploaded, asset.Status);
        Assert.Equal(now, asset.UpdatedAt);
    }

    [Fact]
    public void PlayableKey_WhenPending_IsNull()
    {
        Assert.Null(CreateAsset().PlayableKey);
    }

    [Fact]
    public void PlayableKey_WhenUploadedWithoutPlaybackVersion_IsTheOriginal()
    {
        var asset = CreateAsset();
        asset.MarkUploaded(DateTimeOffset.UtcNow);

        Assert.Equal(asset.SourceKey, asset.PlayableKey);
    }

    [Fact]
    public void MarkUploaded_WhenNotPending_Throws()
    {
        var asset = CreateAsset();
        asset.MarkUploaded(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => asset.MarkUploaded(DateTimeOffset.UtcNow));
    }
}
