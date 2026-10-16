using System.Net;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthEndpointTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
