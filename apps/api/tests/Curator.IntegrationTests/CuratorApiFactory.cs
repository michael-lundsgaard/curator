using System.Net.Http.Headers;
using System.Security.Cryptography;
using Curator.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Curator.IntegrationTests;

/// <summary>
/// Runs the API against a throwaway Postgres container with all migrations applied. Access tokens are signed
/// with a test key instead of coming from Supabase.
/// </summary>
public class CuratorApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SupabaseUrl = "https://curator-tests.supabase.co";
    private const string Issuer = $"{SupabaseUrl}/auth/v1";

    public const string R2AccountId = "curator-tests";
    public const string R2Bucket = "curator-tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>Stands in for the Supabase project's ECC (P-256) signing key.</summary>
    public SecurityKey SigningKey { get; } =
        new ECDsaSecurityKey(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test-ecc" };

    /// <summary>Stands in for Supabase's legacy HS256 secret, which the API must reject.</summary>
    public SecurityKey LegacySecret { get; } =
        new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "test-legacy" };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Curator", _postgres.GetConnectionString());
        builder.UseSetting("Supabase:Url", SupabaseUrl);

        // Presigned URLs are computed locally, so made-up credentials work as long as nothing calls R2.
        builder.UseSetting("R2:AccountId", R2AccountId);
        builder.UseSetting("R2:Bucket", R2Bucket);
        builder.UseSetting("R2:AccessKeyId", "test-access-key");
        builder.UseSetting("R2:SecretAccessKey", "test-secret-key");

        // Serve the keys directly instead of fetching them from {Authority}/.well-known/openid-configuration.
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(SigningKey);
                configuration.SigningKeys.Add(LegacySecret);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
    }

    /// <summary>Creates a client signed in as the given user.</summary>
    public HttpClient CreateClient(Guid userId, string email = "test.user@example.com") =>
        CreateClient(CreateToken(userId, email));

    /// <summary>Creates a client that sends the given access token.</summary>
    public HttpClient CreateClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Creates an access token shaped like Supabase's. Valid unless told otherwise.</summary>
    public string CreateToken(
        Guid userId,
        string email = "test.user@example.com",
        string audience = "authenticated",
        string? subject = null,
        DateTime? expires = null,
        SigningCredentials? signingCredentials = null)
    {
        var expiresAt = expires ?? DateTime.UtcNow.AddHours(1);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject ?? userId.ToString(),
                ["email"] = email,
                ["role"] = "authenticated",
            },
            IssuedAt = expiresAt.AddHours(-1),
            NotBefore = expiresAt.AddHours(-1),
            Expires = expiresAt,
            SigningCredentials = signingCredentials ?? new SigningCredentials(SigningKey, SecurityAlgorithms.EcdsaSha256),
        });
    }

    public CuratorDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<CuratorDbContext>();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<CuratorApiFactory>
{
    public const string Name = "Api";
}
