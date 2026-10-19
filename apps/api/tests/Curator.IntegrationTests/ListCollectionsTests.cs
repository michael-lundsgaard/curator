using System.Net;
using System.Net.Http.Json;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ListCollectionsTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task List_ReturnsTheTreeFlat_WithParentIds()
    {
        var client = await CreateUserAsync();
        var parent = await CreateAsync(client, $"Parent {Guid.NewGuid()}");
        var child = await CreateAsync(client, $"Child {Guid.NewGuid()}", parent.Id);

        var response = await client.GetAsync("/collections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await response.Content.ReadFromJsonAsync<Page>())!.Items;
        Assert.Contains(new Item(parent.Id, null, parent.Name), items);
        Assert.Contains(new Item(child.Id, parent.Id, child.Name), items);
    }

    [Fact]
    public async Task List_SortsByNameIgnoringCase()
    {
        var client = await CreateUserAsync();
        // Other tests' collections are listed too, so only these are compared.
        var suffix = Guid.NewGuid().ToString();
        var b = await CreateAsync(client, $"b {suffix}");
        var a = await CreateAsync(client, $"A {suffix}");
        var c = await CreateAsync(client, $"C {suffix}");

        var items = (await client.GetFromJsonAsync<Page>("/collections"))!.Items;

        Assert.Equal([a.Id, b.Id, c.Id], items.Where(i => i.Name.EndsWith(suffix)).Select(i => i.Id));
    }

    private async Task<HttpClient> CreateUserAsync()
    {
        var client = factory.CreateClient(Guid.NewGuid());
        // The first request creates the profile that collections reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<Item> CreateAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var response = await client.PostAsJsonAsync("/collections", new { name, parentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Item>())!;
    }

    private sealed record Page(IReadOnlyList<Item> Items);

    private sealed record Item(Guid Id, Guid? ParentId, string Name);
}
