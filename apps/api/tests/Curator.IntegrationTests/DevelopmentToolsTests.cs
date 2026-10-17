using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Curator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

/// <summary>Endpoints that only exist in Development, which is the test host's environment.</summary>
[Collection(ApiCollection.Name)]
public class DevelopmentToolsTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task SampleData_AddsClipsOnce_IncludingSomeByTheCaller()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // Another test may have added the samples already.
        (await client.DeleteAsync("/dev/data")).EnsureSuccessStatusCode();

        var first = await client.PostAsync("/dev/sample-data", null);
        var second = await client.PostAsync("/dev/sample-data", null);

        first.EnsureSuccessStatusCode();
        Assert.Equal(SampleData.ClipCount, (await first.Content.ReadFromJsonAsync<Added>())!.Count);
        Assert.Equal(0, (await second.Content.ReadFromJsonAsync<Added>())!.Count);
        var mine = await client.GetFromJsonAsync<JsonElement>($"/assets?uploadedBy={userId}");
        // The caller's abandoned upload isn't listed.
        Assert.Equal(SampleData.CallerClipCount, mine.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ClearData_DeletesAllAssetsAndSampleProfiles_ButKeepsTheCaller()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        (await client.PostAsync("/dev/sample-data", null)).EnsureSuccessStatusCode();
        await using var db = factory.CreateDbContext();
        // Other tests' assets, deleted and pending ones included, go too.
        var assets = await db.Assets.IgnoreQueryFilters().CountAsync();

        var response = await client.DeleteAsync("/dev/data");

        response.EnsureSuccessStatusCode();
        Assert.Equal(assets, (await response.Content.ReadFromJsonAsync<Deleted>())!.Count);
        Assert.False(await db.Assets.IgnoreQueryFilters().AnyAsync());
        Assert.True(await db.Profiles.AnyAsync(p => p.Id == userId));
        // The sample profiles are gone, so the samples can be added again.
        var again = await client.PostAsync("/dev/sample-data", null);
        Assert.Equal(SampleData.ClipCount, (await again.Content.ReadFromJsonAsync<Added>())!.Count);
    }

    [Fact]
    public async Task ClearData_WithHostedDatabase_RefusesBeforeConnecting()
    {
        var options = new DbContextOptionsBuilder<CuratorDbContext>()
            .UseNpgsql("Host=db.curator-tests.supabase.co;Database=postgres;Username=postgres;Password=secret")
            .Options;
        await using var db = new CuratorDbContext(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SampleData.ClearAsync(db, CancellationToken.None));
    }

    [Fact]
    public async Task OpenApiDocument_DeclaresBearerTokens_SoScalarCanSendThem()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
    }

    private sealed record Added([property: JsonPropertyName("added")] int Count);

    private sealed record Deleted([property: JsonPropertyName("deleted")] int Count);
}
