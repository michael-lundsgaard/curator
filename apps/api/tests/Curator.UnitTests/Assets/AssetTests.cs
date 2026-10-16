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
}
