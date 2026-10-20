using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Curator.IntegrationTests;

/// <summary>
/// The public OpenAPI document (<c>v1</c>), served in Development (the test host's environment). It's committed as
/// <c>apps/api/openapi.json</c>, which the app's API client is generated from.
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

    [Fact]
    public async Task DeclaresBearerTokens_SoScalarCanSendThem()
    {
        var document = await GetDocumentAsync();

        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
    }

    [Fact]
    public async Task DescribesEnumsAsStringsByName()
    {
        var document = await GetDocumentAsync();

        var sort = Schemas(document).GetProperty("AssetSort");
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

    [Fact]
    public async Task GivesEachUseCaseItsOwnSchemas_NamedAsRequestsAndResponses()
    {
        var document = await GetDocumentAsync();

        var schemas = Schemas(document);
        Assert.True(schemas.GetProperty("EditAssetRequest").GetProperty("properties").TryGetProperty("title", out _));
        Assert.True(
            schemas.GetProperty("CreateCollectionRequest").GetProperty("properties").TryGetProperty("name", out _));
        Assert.DoesNotContain(schemas.EnumerateObject(), s => s.Name.EndsWith("Command") || s.Name.EndsWith("Query"));
    }

    [Fact]
    public async Task DescribesEveryPropertyOfOurSchemas()
    {
        var document = await GetDocumentAsync();

        var undocumented = Schemas(document).EnumerateObject()
            .Where(schema => !schema.Name.EndsWith("ProblemDetails"))
            .SelectMany(schema => schema.Value.TryGetProperty("properties", out var properties)
                ? properties.EnumerateObject()
                    .Where(property => !property.Value.TryGetProperty("description", out _))
                    .Select(property => $"{schema.Name}.{property.Name}")
                : [])
            .ToList();
        Assert.Empty(undocumented);
    }

    // Path parameters are exempt: the path already says what they are (/assets/{id}).
    [Fact]
    public async Task DescribesEveryParameter_ExceptPathParameters()
    {
        var document = await GetDocumentAsync();

        var undocumented = Operations(document)
            .SelectMany(o => o.Operation.TryGetProperty("parameters", out var parameters)
                ? parameters.EnumerateArray()
                    .Where(parameter => parameter.GetProperty("in").GetString() != "path"
                        && !parameter.TryGetProperty("description", out _))
                    .Select(parameter => $"{o.Method} {o.Path} {parameter.GetProperty("name").GetString()}")
                : [])
            .ToList();
        Assert.Empty(undocumented);
    }

    [Fact]
    public async Task KeepsSummariesAndDescriptionsOnOneLine_SoTheFileIsTheSameOnEveryOs()
    {
        var document = await GetDocumentAsync();

        var multiline = Texts(document).Where(text => text.Contains('\n') || text.Contains('\r')).ToList();
        Assert.Empty(multiline);
    }

    [Fact]
    public async Task IncludesValidationLimits_OfBodiesAndQueryParameters()
    {
        var document = await GetDocumentAsync();

        var title = Schemas(document).GetProperty("EditAssetRequest").GetProperty("properties").GetProperty("title");
        Assert.Equal(1, title.GetProperty("minLength").GetInt32());
        Assert.Equal(255, title.GetProperty("maxLength").GetInt32());

        var assetIds = Schemas(document).GetProperty("AddClipsToCollectionRequest")
            .GetProperty("properties").GetProperty("assetIds");
        Assert.Equal(1, assetIds.GetProperty("minItems").GetInt32());
        Assert.Equal(100, assetIds.GetProperty("maxItems").GetInt32());

        var limit = QueryParameter(document, "/assets", "limit").GetProperty("schema");
        Assert.Equal(1, limit.GetProperty("minimum").GetInt32());
        Assert.Equal(100, limit.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public async Task NamesQueryParametersInCamelCase_AndTypesNumbersAsNumbers()
    {
        var document = await GetDocumentAsync();

        var parameters = document.GetProperty("paths").GetProperty("/assets").GetProperty("get")
            .GetProperty("parameters").EnumerateArray().Select(parameter => parameter.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("includeSubCollections", parameters);
        Assert.All(parameters, name => Assert.True(char.IsLower(name![0]), $"{name} isn't camelCase."));

        var limit = QueryParameter(document, "/assets", "limit").GetProperty("schema");
        Assert.Equal("integer", limit.GetProperty("type").GetString());
    }

    [Fact]
    public async Task DescribesBodiesAsJson_AndErrorsAsProblemJson()
    {
        var document = await GetDocumentAsync();

        var requests = Operations(document)
            .Where(o => o.Operation.TryGetProperty("requestBody", out _))
            .ToList();
        Assert.NotEmpty(requests);
        Assert.All(requests, r => Assert.Equal(
            ["application/json"],
            r.Operation.GetProperty("requestBody").GetProperty("content").EnumerateObject()
                .Select(content => content.Name)));

        var responses = Operations(document)
            .SelectMany(o => o.Operation.GetProperty("responses").EnumerateObject()
                .Where(response => response.Value.TryGetProperty("content", out _))
                .Select(response => (o.Method, o.Path, Status: response.Name, Response: response.Value)))
            .ToList();
        Assert.All(responses, r =>
        {
            var contentType = r.Status.StartsWith('4') ? "application/problem+json" : "application/json";
            Assert.Equal(
                [contentType],
                r.Response.GetProperty("content").EnumerateObject().Select(content => content.Name));
        });
    }

    // LF and one final newline, however the file was checked out.
    private static string Normalize(string json) => json.ReplaceLineEndings("\n").TrimEnd() + "\n";

    // From this file's path at compile time, since the test runs from bin/ or a custom --artifacts-path. A
    // deterministic CI build (ContinuousIntegrationBuild, PathMap) maps that path to /_/, which would break this.
    private static string CommittedFilePath([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "openapi.json"));

    private async Task<JsonElement> GetDocumentAsync() =>
        await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

    private static JsonElement Schemas(JsonElement document) =>
        document.GetProperty("components").GetProperty("schemas");

    private static IEnumerable<(string Method, string Path, JsonElement Operation)> Operations(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Select(operation => (operation.Name.ToUpperInvariant(), path.Name, operation.Value)));

    // Every summary and description anywhere in the document.
    private static IEnumerable<string> Texts(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().SelectMany(property =>
            property.Name is "summary" or "description" && property.Value.ValueKind == JsonValueKind.String
                ? [property.Value.GetString()!]
                : Texts(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(Texts),
        _ => [],
    };

    private static JsonElement QueryParameter(JsonElement document, string path, string name) =>
        document.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == name);
}
