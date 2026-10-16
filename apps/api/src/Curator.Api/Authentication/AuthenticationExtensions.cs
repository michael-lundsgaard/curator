using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Curator.Api.Authentication;

public static class AuthenticationExtensions
{
    /// <summary>Validates Supabase access tokens and requires a signed-in user on every endpoint by default.</summary>
    public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Resolved lazily so tests can override the configuration.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var supabaseUrl = configuration["Supabase:Url"];
                if (string.IsNullOrWhiteSpace(supabaseUrl))
                {
                    throw new InvalidOperationException("Supabase:Url is not configured.");
                }

                // Issuer and signing keys come from {Authority}/.well-known/openid-configuration.
                options.Authority = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
                options.Audience = "authenticated";

                // Keep claim names as issued ("sub", "email"), so Supabase's own "role" claim isn't mistaken for
                // an ASP.NET role.
                options.MapInboundClaims = false;

                // Only the project's ECC (P-256) keys, never HS256 with the legacy shared secret.
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256];

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!Guid.TryParse(context.Principal?.FindFirst(SupabaseClaims.Subject)?.Value, out _))
                        {
                            context.Fail("The sub claim is not a user ID.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
