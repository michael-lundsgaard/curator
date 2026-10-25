using System.Net;
using System.Net.Http.Json;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ListProfilesTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task List_ReturnsEveryoneSortedByNameIgnoringCase_WithoutRoles()
    {
        // Other tests' profiles are listed too, so only these are compared.
        var suffix = Guid.NewGuid().ToString("N");
        var b = await CreateUserAsync($"b{suffix}");
        var a = await CreateUserAsync($"A{suffix}");
        var c = await CreateUserAsync($"C{suffix}");

        var response = await factory.CreateClient(Guid.NewGuid()).GetAsync("/profiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("role", json, StringComparison.OrdinalIgnoreCase);
        var items = (await response.Content.ReadFromJsonAsync<Page>())!.Items;
        Assert.Equal(
            [new Item(a, $"A{suffix}"), new Item(b, $"b{suffix}"), new Item(c, $"C{suffix}")],
            items.Where(i => i.DisplayName.EndsWith(suffix)));
    }

    private async Task<Guid> CreateUserAsync(string name)
    {
        var userId = Guid.NewGuid();
        // The first request creates the profile, named after the email address.
        (await factory.CreateClient(userId, $"{name}@example.com").GetAsync("/me")).EnsureSuccessStatusCode();
        return userId;
    }

    private sealed record Page(IReadOnlyList<Item> Items);

    private sealed record Item(Guid Id, string DisplayName);
}
