using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Curator.IntegrationTests;

[Collection(ApiCollection.Name)]
public class MeEndpointTests(CuratorApiFactory factory)
{
    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_OnFirstRequest_CreatesMemberProfile()
    {
        var userId = Guid.NewGuid();

        var response = await factory.CreateClient(userId, "alice@example.com").GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new MeResponse(userId, "alice", "Member"), await response.Content.ReadFromJsonAsync<MeResponse>());
    }

    [Fact]
    public async Task Me_OnParallelFirstRequests_CreatesOneProfile()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateClient(userId);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.GetAsync("/me")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.Profiles.CountAsync(p => p.Id == userId));
    }

    public static TheoryData<string> InvalidTokens =>
        ["unknown key", "legacy secret", "wrong audience", "expired", "sub not a GUID"];

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Me_WithInvalidToken_ReturnsUnauthorizedAndCreatesNoProfile(string invalid)
    {
        var userId = Guid.NewGuid();
        var token = invalid switch
        {
            "unknown key" => factory.CreateToken(userId, signingCredentials: new SigningCredentials(
                new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test-ecc" },
                SecurityAlgorithms.EcdsaSha256)),
            "legacy secret" => factory.CreateToken(userId, signingCredentials: new SigningCredentials(
                factory.LegacySecret, SecurityAlgorithms.HmacSha256)),
            "wrong audience" => factory.CreateToken(userId, audience: "anon"),
            "expired" => factory.CreateToken(userId, expires: DateTime.UtcNow.AddMinutes(-10)),
            "sub not a GUID" => factory.CreateToken(userId, subject: "not-a-guid"),
            _ => throw new ArgumentOutOfRangeException(nameof(invalid)),
        };

        var response = await factory.CreateClient(token).GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var db = factory.CreateDbContext();
        Assert.False(await db.Profiles.AnyAsync(p => p.Id == userId));
    }

    private sealed record MeResponse(Guid Id, string DisplayName, string Role);
}
