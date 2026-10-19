using System.Net;
using System.Net.Http.Json;
using Curator.Domain.Collections;
using Curator.Domain.Profiles;
using Microsoft.EntityFrameworkCore;

namespace Curator.IntegrationTests;

/// <summary>Editing, moving and deleting collections, which only the creator and admins may do.</summary>
[Collection(ApiCollection.Name)]
public class ChangeCollectionTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Edit_ByCreator_SavesNameAndClearsMissingDescription()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams", description: "Loud ones");

        var response = await client.PatchAsJsonAsync($"/collections/{collection.Id}", new { name = " Jumpscares " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = (await response.Content.ReadFromJsonAsync<Created>())!;
        Assert.Equal("Jumpscares", edited.Name);
        Assert.Null(edited.Description);
        await using var db = factory.CreateDbContext();
        Assert.Equal("Jumpscares", (await db.Collections.SingleAsync(c => c.Id == collection.Id)).Name);
    }

    [Fact]
    public async Task Edit_ByAdmin_IsAllowed()
    {
        var (creator, _) = await CreateUserAsync();
        var collection = await CreateAsync(creator, "Screams");
        var (admin, _) = await CreateUserAsync(ProfileRole.Admin);

        var response = await admin.PatchAsJsonAsync($"/collections/{collection.Id}", new { name = "Jumpscares" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_ByAnotherMember_ReturnsForbidden()
    {
        var (creator, _) = await CreateUserAsync();
        var collection = await CreateAsync(creator, "Screams");
        var (other, _) = await CreateUserAsync();

        var response = await other.PatchAsJsonAsync($"/collections/{collection.Id}", new { name = "Mine now" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Edit_WithoutName_ReturnsBadRequest()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");

        var response = await client.PatchAsJsonAsync($"/collections/{collection.Id}", new { name = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Unknown_ReturnsNotFound()
    {
        var (client, _) = await CreateUserAsync();

        var response = await client.PatchAsJsonAsync($"/collections/{Guid.NewGuid()}", new { name = "Screams" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Move_IntoAnotherUsersCollection_AndBackToTheTop()
    {
        var (other, _) = await CreateUserAsync();
        var target = await CreateAsync(other, "Best of");
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");
        var child = await CreateAsync(client, "Loudest", collection.Id);

        var inside = await client.PostAsJsonAsync($"/collections/{collection.Id}/move", new { parentId = target.Id });
        var top = await client.PostAsJsonAsync($"/collections/{child.Id}/move", new { parentId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, inside.StatusCode);
        Assert.Equal(target.Id, (await inside.Content.ReadFromJsonAsync<Created>())!.ParentId);
        Assert.Equal(HttpStatusCode.OK, top.StatusCode);
        await using var db = factory.CreateDbContext();
        Assert.Equal(target.Id, (await db.Collections.SingleAsync(c => c.Id == collection.Id)).ParentId);
        Assert.Null((await db.Collections.SingleAsync(c => c.Id == child.Id)).ParentId);
    }

    [Fact]
    public async Task Move_IntoItselfOrItsSubCollection_ReturnsConflict()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");
        var child = await CreateAsync(client, "Loudest", collection.Id);
        var grandchild = await CreateAsync(client, "Ever", child.Id);

        var self = await client.PostAsJsonAsync($"/collections/{collection.Id}/move", new { parentId = collection.Id });
        var below = await client.PostAsJsonAsync(
            $"/collections/{collection.Id}/move", new { parentId = grandchild.Id });

        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, below.StatusCode);
    }

    [Fact]
    public async Task Move_WhenSubCollectionsWouldBeTooDeep_ReturnsConflict()
    {
        var (client, _) = await CreateUserAsync();
        // A target at depth MaxDepth - 1, and a collection with one sub-collection: one level too many.
        Guid? targetId = null;
        for (var depth = 1; depth < Collection.MaxDepth; depth++)
        {
            targetId = (await CreateAsync(client, $"Level {depth}", targetId)).Id;
        }

        var collection = await CreateAsync(client, "Screams");
        await CreateAsync(client, "Loudest", collection.Id);

        var response = await client.PostAsJsonAsync(
            $"/collections/{collection.Id}/move", new { parentId = targetId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Move_IntoMissingParent_ReturnsBadRequest()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");

        var response = await client.PostAsJsonAsync(
            $"/collections/{collection.Id}/move", new { parentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Move_ByAnotherMember_ReturnsForbidden()
    {
        var (creator, _) = await CreateUserAsync();
        var collection = await CreateAsync(creator, "Screams");
        var (other, _) = await CreateUserAsync();
        var target = await CreateAsync(other, "Mine");

        var response = await other.PostAsJsonAsync($"/collections/{collection.Id}/move", new { parentId = target.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Move_WaitsWhileAnotherRequestHoldsTheTreeLock()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");
        var target = await CreateAsync(client, "Best of");
        await using var db = factory.CreateDbContext();
        var held = await db.LockCollectionTreeAsync(CancellationToken.None);

        var move = client.PostAsJsonAsync($"/collections/{collection.Id}/move", new { parentId = target.Id });

        // Still waiting after a moment, because this test holds the lock.
        Assert.NotSame(move, await Task.WhenAny(move, Task.Delay(TimeSpan.FromMilliseconds(500))));
        await held.RollbackAsync();
        await held.DisposeAsync();
        Assert.Equal(HttpStatusCode.OK, (await move).StatusCode);
    }

    [Fact]
    public async Task Move_TwoIntoEachOtherAtOnce_OnlyOneSucceeds()
    {
        var (client, _) = await CreateUserAsync();
        var a = await CreateAsync(client, "A");
        var b = await CreateAsync(client, "B");

        // Without the lock both could pass their checks before either saves, leaving A and B inside each other.
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync($"/collections/{a.Id}/move", new { parentId = b.Id }),
            client.PostAsJsonAsync($"/collections/{b.Id}/move", new { parentId = a.Id }));

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).Order());
    }

    [Fact]
    public async Task Delete_ByCreator_DeletesSubCollectionsByOthersToo()
    {
        var (client, _) = await CreateUserAsync();
        var collection = await CreateAsync(client, "Screams");
        var (other, _) = await CreateUserAsync();
        var child = await CreateAsync(other, "Loudest", collection.Id);
        var grandchild = await CreateAsync(other, "Ever", child.Id);

        var response = await client.DeleteAsync($"/collections/{collection.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Deleted(collection.Id, 3), await response.Content.ReadFromJsonAsync<Deleted>());
        await using var db = factory.CreateDbContext();
        Guid[] deleted = [collection.Id, child.Id, grandchild.Id];
        Assert.False(await db.Collections.AnyAsync(c => deleted.Contains(c.Id)));
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/collections/{collection.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_ByAdmin_IsAllowed()
    {
        var (creator, _) = await CreateUserAsync();
        var collection = await CreateAsync(creator, "Screams");
        var (admin, _) = await CreateUserAsync(ProfileRole.Admin);

        var response = await admin.DeleteAsync($"/collections/{collection.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ByAnotherMember_ReturnsForbidden()
    {
        var (creator, _) = await CreateUserAsync();
        var collection = await CreateAsync(creator, "Screams");
        var (other, _) = await CreateUserAsync();

        var response = await other.DeleteAsync($"/collections/{collection.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateUserAsync(ProfileRole role = ProfileRole.Member)
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);
        // The first request creates the profile that collections reference.
        (await client.GetAsync("/me")).EnsureSuccessStatusCode();
        if (role != ProfileRole.Member)
        {
            await using var db = factory.CreateDbContext();
            await db.Profiles.Where(p => p.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Role, role));
        }

        return (client, userId);
    }

    private static async Task<Created> CreateAsync(
        HttpClient client,
        string name,
        Guid? parentId = null,
        string? description = null)
    {
        var response = await client.PostAsJsonAsync("/collections", new { name, description, parentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    private sealed record Created(Guid Id, Guid? ParentId, string Name, string? Description);

    private sealed record Deleted(Guid Id, int DeletedCount);
}
