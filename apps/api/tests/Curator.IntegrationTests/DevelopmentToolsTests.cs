using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Curator.Api.Development;
using Curator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

/// <summary>Endpoints that only exist in Development, which is the test host's environment.</summary>
[Collection(ApiCollection.Name)]
public class DevelopmentToolsTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task SampleData_AddsClipsAndCollectionsOnce_IncludingSomeByTheCaller()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        var sample = new SampleData(userId, DateTimeOffset.UtcNow);
        // Another test may have added the samples already.
        (await client.DeleteAsync("/dev/data")).EnsureSuccessStatusCode();

        var first = await client.PostAsync("/dev/sample-data", null);
        var second = await client.PostAsync("/dev/sample-data", null);

        first.EnsureSuccessStatusCode();
        Assert.Equal(
            new Added(sample.Clips.Count, sample.Collections.Count),
            await first.Content.ReadFromJsonAsync<Added>());
        Assert.Equal(new Added(0, 0), await second.Content.ReadFromJsonAsync<Added>());
        var mine = await client.GetFromJsonAsync<JsonElement>($"/assets?uploadedBy={userId}");
        // The caller's abandoned upload isn't listed.
        Assert.Equal(
            sample.Clips.Count(c => c.UploadedBy == userId), mine.GetProperty("items").GetArrayLength());
        var collections = await client.GetFromJsonAsync<JsonElement>("/collections");
        Assert.Equal(sample.Collections.Count, collections.GetProperty("items").GetArrayLength());
        Assert.Equal(
            sample.CollectionClips.Count,
            collections.GetProperty("items").EnumerateArray().Sum(c => c.GetProperty("clipCount").GetInt32()));
    }

    [Fact]
    public async Task ClearData_DeletesAllAssetsAndSampleProfiles_ButKeepsTheCaller()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        (await client.PostAsync("/dev/sample-data", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/collections", new { name = "Screams" })).EnsureSuccessStatusCode();
        await using var db = factory.CreateDbContext();
        // Other tests' assets, deleted and pending ones included, go too.
        var assets = await db.Assets.IgnoreQueryFilters().CountAsync();

        var response = await client.DeleteAsync("/dev/data");

        response.EnsureSuccessStatusCode();
        Assert.Equal(assets, (await response.Content.ReadFromJsonAsync<Deleted>())!.Count);
        Assert.False(await db.Assets.IgnoreQueryFilters().AnyAsync());
        Assert.False(await db.Collections.AnyAsync());
        Assert.True(await db.Profiles.AnyAsync(p => p.Id == userId));
        // The sample profiles are gone, so the samples can be added again.
        var again = await client.PostAsync("/dev/sample-data", null);
        Assert.Equal(
            new SampleData(userId, DateTimeOffset.UtcNow).Clips.Count,
            (await again.Content.ReadFromJsonAsync<Added>())!.Clips);
    }

    [Fact]
    public async Task ClearData_WithHostedDatabase_RefusesBeforeConnecting()
    {
        var options = new DbContextOptionsBuilder<CuratorDbContext>()
            .UseNpgsql("Host=db.curator-tests.supabase.co;Database=postgres;Username=postgres;Password=secret")
            .Options;
        await using var db = new CuratorDbContext(options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DevelopmentData.ClearAsync(db, CancellationToken.None));
    }

    private sealed record Added(int Clips, int Collections);

    private sealed record Deleted([property: JsonPropertyName("deleted")] int Count);
}
