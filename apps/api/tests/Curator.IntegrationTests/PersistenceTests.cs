using Curator.Domain.Assets;
using Curator.Domain.Profiles;
using Curator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PersistenceTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Asset_StoresStatusAsText()
    {
        var asset = await SaveAssetAsync();

        await using var db = CreateDbContext();
        var status = await db.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM assets WHERE id = {asset.Id}")
            .SingleAsync();

        Assert.Equal("Pending", status);
    }

    [Fact]
    public async Task DeletedAssets_AreHiddenFromQueries()
    {
        var asset = await SaveAssetAsync();

        await using (var db = CreateDbContext())
        {
            var saved = await db.Assets.SingleAsync(a => a.Id == asset.Id);
            saved.Delete(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        await using (var db = CreateDbContext())
        {
            Assert.False(await db.Assets.AnyAsync(a => a.Id == asset.Id));
            Assert.True(await db.Assets.IgnoreQueryFilters().AnyAsync(a => a.Id == asset.Id));
        }
    }

    private async Task<Asset> SaveAssetAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = new Profile(Guid.NewGuid(), "Test user", now);
        var asset = new Asset(profile.Id, "Clip", "clip.mp4", "video/mp4", "originals/clip.mp4", 1024, now, now);

        await using var db = CreateDbContext();
        db.AddRange(profile, asset);
        await db.SaveChangesAsync();
        return asset;
    }

    private CuratorDbContext CreateDbContext() => factory.CreateDbContext();
}
