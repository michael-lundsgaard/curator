using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Curator.IntegrationTests;

/// <summary>Errors ASP.NET Core answers itself come as ProblemDetails too, like the handlers' errors.</summary>
[Collection(ApiCollection.Name)]
public class ErrorResponseTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task UnmatchedRoute_ReturnsNotFoundAsProblemDetails()
    {
        var client = factory.CreateClient(Guid.NewGuid());

        // The route only matches GUIDs, so this never reaches the controller.
        var response = await client.GetAsync("/collections/1");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WithoutToken_ReturnsUnauthorizedAsProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/collections");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal((int)status, problem!.Status);
    }
}
