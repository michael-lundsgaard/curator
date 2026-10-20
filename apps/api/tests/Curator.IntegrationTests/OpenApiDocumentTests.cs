using System.Net.Http.Json;
using System.Text.Json;

namespace Curator.IntegrationTests;

/// <summary>
/// The OpenAPI document, served in Development (the test host's environment). The same document is written to
/// <c>apps/api/openapi.json</c> on build, and the app's API client is generated from it.
/// </summary>
[Collection(ApiCollection.Name)]
public class OpenApiDocumentTests(CuratorApiFactory factory)
{
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

    [Fact]
    public async Task IncludesValidationLimits_OfBodiesAndQueryParameters()
    {
        var document = await GetDocumentAsync();

        var title = Schemas(document).GetProperty("EditAssetRequest").GetProperty("properties").GetProperty("title");
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

    private async Task<JsonElement> GetDocumentAsync() =>
        await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

    private static JsonElement Schemas(JsonElement document) =>
        document.GetProperty("components").GetProperty("schemas");

    private static IEnumerable<(string Method, string Path, JsonElement Operation)> Operations(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Select(operation => (operation.Name.ToUpperInvariant(), path.Name, operation.Value)));

    private static JsonElement QueryParameter(JsonElement document, string path, string name) =>
        document.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == name);
}
