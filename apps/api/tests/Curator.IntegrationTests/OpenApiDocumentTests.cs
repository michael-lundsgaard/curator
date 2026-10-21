using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Curator.IntegrationTests;

/// <summary>
/// The public OpenAPI document (<c>v1</c>), served in Development (the test host's environment). It's committed as
/// <c>apps/api/openapi.json</c>, which the app's API client is generated from. How the document looks is checked by
/// comparing it with that file; the other tests check rules the app's client relies on.
/// </summary>
[Collection(ApiCollection.Name)]
public class OpenApiDocumentTests(CuratorApiFactory factory)
{
    private const string UpdateVariable = "UPDATE_OPENAPI";

    // Only compares, so a stale file keeps failing until it's updated on purpose and committed.
    [Fact]
    public async Task MatchesTheCommittedFile()
    {
        var served = Normalize(await factory.CreateClient().GetStringAsync("/openapi/v1.json"));
        var path = CommittedFilePath();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, served);
            return;
        }

        var committed = File.Exists(path) ? Normalize(await File.ReadAllTextAsync(path)) : "";
        Assert.True(
            served == committed,
            $"apps/api/openapi.json is out of date. Update it with `dotnet test tests/Curator.IntegrationTests " +
            $"--filter {nameof(MatchesTheCommittedFile)} -e {UpdateVariable}=1` (from apps/api), check the diff and " +
            "commit it with the API change.");
    }

    [Fact]
    public async Task LeavesTheDevelopmentEndpointsToTheirOwnDocument()
    {
        var client = factory.CreateClient();
        var v1 = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var dev = await client.GetFromJsonAsync<JsonElement>("/openapi/dev.json");

        Assert.DoesNotContain(v1.GetProperty("paths").EnumerateObject(), path => path.Name.StartsWith("/dev"));
        Assert.All(dev.GetProperty("paths").EnumerateObject(), path => Assert.StartsWith("/dev", path.Name));
        Assert.NotEmpty(dev.GetProperty("paths").EnumerateObject());
    }

    // The API sends enums by name, so the client's types must be those names.
    [Fact]
    public async Task DescribesEnumsAsStringsByName()
    {
        var document = await GetDocumentAsync();

        var sort = document.GetProperty("components").GetProperty("schemas").GetProperty("AssetSort");
        Assert.Equal("string", sort.GetProperty("type").GetString());
        Assert.Equal(
            ["Recorded", "Uploaded", "Title"],
            sort.GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
    }

    [Fact]
    public async Task NamesEveryOperation_SoTheGeneratedClientHasReadableFunctions()
    {
        var document = await GetDocumentAsync();

        var operations = Operations(document).ToList();
        Assert.All(operations, o => Assert.True(
            o.Operation.TryGetProperty("operationId", out _), $"{o.Method} {o.Path} has no operationId."));
        Assert.Contains(operations, o => o.Operation.GetProperty("operationId").GetString() == "EditAsset");
    }

    // The client sends query parameters under the names in the document.
    [Fact]
    public async Task NamesQueryParametersInCamelCase_AndTypesNumbersAsNumbers()
    {
        var document = await GetDocumentAsync();

        var parameters = document.GetProperty("paths").GetProperty("/assets").GetProperty("get")
            .GetProperty("parameters").EnumerateArray().ToList();
        var names = parameters.Select(parameter => parameter.GetProperty("name").GetString()).ToList();
        Assert.Contains("includeSubCollections", names);
        Assert.All(names, name => Assert.True(char.IsLower(name![0]), $"{name} isn't camelCase."));

        var limit = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "limit");
        Assert.Equal("integer", limit.GetProperty("schema").GetProperty("type").GetString());
    }

    // LF and one final newline, however the file was checked out.
    private static string Normalize(string json) => json.ReplaceLineEndings("\n").TrimEnd() + "\n";

    // From this file's path at compile time, since the test runs from bin/ or a custom --artifacts-path. A
    // deterministic CI build (ContinuousIntegrationBuild, PathMap) maps that path to /_/, which would break this.
    private static string CommittedFilePath([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "openapi.json"));

    private async Task<JsonElement> GetDocumentAsync() =>
        await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

    private static IEnumerable<(string Method, string Path, JsonElement Operation)> Operations(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Select(operation => (operation.Name.ToUpperInvariant(), path.Name, operation.Value)));
}
