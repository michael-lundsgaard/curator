using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        var first = await client.PostAsync("/dev/sample-data", null);
        var second = await client.PostAsync("/dev/sample-data", null);

        first.EnsureSuccessStatusCode();
        Assert.Equal(18, (await first.Content.ReadFromJsonAsync<Added>())!.Count);
        Assert.Equal(0, (await second.Content.ReadFromJsonAsync<Added>())!.Count);
        var mine = await client.GetFromJsonAsync<JsonElement>($"/assets?uploadedBy={userId}");
        // The caller's abandoned upload isn't listed.
        Assert.Equal(4, mine.GetProperty("items").GetArrayLength());
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
}
