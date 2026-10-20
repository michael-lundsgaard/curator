using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Collections;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CreateCollectionTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Create_AtTopLevel_SavesItAndPointsToIt()
    {
        var (client, userId) = await CreateUserAsync();

        // parentId is left out: optional fields of a create request have a default.
        var response = await client.PostAsJsonAsync(
            "/collections", new { name = " R.E.P.O ", description = "Monster-filled evenings" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<Created>())!;
        Assert.Equal("R.E.P.O", created.Name);
        Assert.Equal("Monster-filled evenings", created.Description);
        Assert.Null(created.ParentId);
        Assert.Equal($"/collections/{created.Id}", response.Headers.Location?.AbsolutePath);

        await using var db = factory.CreateDbContext();
        var saved = await db.Collections.SingleAsync(c => c.Id == created.Id);
        Assert.Equal(userId, saved.CreatedBy);
    }

    [Fact]
    public async Task Create_InsideAnotherUsersCollection_IsAllowed()
    {
        var (owner, _) = await CreateUserAsync();
        var parent = await CreateAsync(owner, "R.E.P.O");
        var (client, _) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/collections", new { name = "Funny moments", parentId = parent.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(parent.Id, (await response.Content.ReadFromJsonAsync<Created>())!.ParentId);
    }

    [Fact]
    public async Task Create_DeeperThanMaxDepth_ReturnsConflict()
    {
        var (client, _) = await CreateUserAsync();
        Guid? parentId = null;
        for (var depth = 1; depth <= Collection.MaxDepth; depth++)
        {
            parentId = (await CreateAsync(client, $"Level {depth}", parentId)).Id;
        }

        var response = await client.PostAsJsonAsync("/collections", new { name = "Too deep", parentId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_InsideMissingParent_ReturnsBadRequest()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/collections", new { name = "Orphan", parentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Create_WithoutName_ReturnsBadRequest(string? name)
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.PostAsJsonAsync("/collections", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsCollectionWithCreator()
    {
        var (client, userId) = await CreateUserAsync();
        var created = await CreateAsync(client, "Screams");

        var response = await client.GetAsync($"/collections/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var collection = (await response.Content.ReadFromJsonAsync<Got>())!;
        Assert.Equal("Screams", collection.Name);
        Assert.Equal(userId, collection.CreatedBy.Id);
        Assert.False(string.IsNullOrEmpty(collection.CreatedBy.DisplayName));
    }

    [Fact]
    public async Task Get_Unknown_ReturnsNotFound()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.GetAsync($"/collections/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that collections reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        return (client, userId);
    }

    private static async Task<Created> CreateAsync(HttpClient client, string name, Guid? parentId = null)
    {
        var response = await client.PostAsJsonAsync("/collections", new { name, parentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    private sealed record Created(Guid Id, Guid? ParentId, string Name, string? Description);

    private sealed record Person(Guid Id, string DisplayName);

    private sealed record Got(Guid Id, string Name, Person CreatedBy);
}
