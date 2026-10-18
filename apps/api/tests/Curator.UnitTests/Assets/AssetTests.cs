using Curator.Domain.Assets;
using Curator.Domain.Profiles;

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

    [Fact]
    public void Edit_TrimsAndReplacesDetails()
    {
        var asset = CreateAsset();
        var recordedAt = new DateTimeOffset(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
        var now = asset.CreatedAt.AddMinutes(1);

        asset.Edit("  Screams  ", " The best one ", recordedAt, now);

        Assert.Equal("Screams", asset.Title);
        Assert.Equal("The best one", asset.Description);
        Assert.Equal(recordedAt, asset.RecordedAt);
        Assert.Equal(now, asset.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Edit_WithBlankDescription_ClearsIt(string? description)
    {
        var asset = CreateAsset();
        asset.Edit("Title", "Something", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        asset.Edit("Title", description, null, DateTimeOffset.UtcNow);

        Assert.Null(asset.Description);
        Assert.Null(asset.RecordedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Edit_WithBlankTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() => CreateAsset().Edit(title, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CanBeEditedBy_UploaderAndAdmins()
    {
        var uploader = new Profile(Guid.NewGuid(), "uploader", DateTimeOffset.UtcNow);
        var member = new Profile(Guid.NewGuid(), "member", DateTimeOffset.UtcNow);
        var admin = new Profile(Guid.NewGuid(), "admin", DateTimeOffset.UtcNow);
        admin.ChangeRole(ProfileRole.Admin);
        var asset = new Asset(uploader.Id, "clip.mp4", "video/mp4", 1024, null, DateTimeOffset.UtcNow);

        Assert.True(asset.CanBeEditedBy(uploader));
        Assert.False(asset.CanBeEditedBy(member));
        Assert.True(asset.CanBeEditedBy(admin));
    }

    [Fact]
    public void Delete_AgainKeepsTheFirstTime()
    {
        var asset = CreateAsset();
        var first = DateTimeOffset.UtcNow;

        asset.Delete(first);
        asset.Delete(first.AddMinutes(1));

        Assert.Equal(first, asset.DeletedAt);
    }

    [Fact]
    public void IsVisibleTo_WhenPending_OnlyTheUploader()
    {
        var uploader = new Profile(Guid.NewGuid(), "uploader", DateTimeOffset.UtcNow);
        var admin = new Profile(Guid.NewGuid(), "admin", DateTimeOffset.UtcNow);
        admin.ChangeRole(ProfileRole.Admin);
        var asset = new Asset(uploader.Id, "clip.mp4", "video/mp4", 1024, null, DateTimeOffset.UtcNow);

        Assert.True(asset.IsVisibleTo(uploader));
        Assert.False(asset.IsVisibleTo(admin));

        asset.MarkUploaded(DateTimeOffset.UtcNow);

        Assert.True(asset.IsVisibleTo(admin));
    }
}
