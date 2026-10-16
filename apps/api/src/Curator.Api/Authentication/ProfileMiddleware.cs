using System.Security.Claims;
using Curator.Application.Profiles;
using Curator.Domain.Profiles;
using Microsoft.AspNetCore.Http.Features;

namespace Curator.Api.Authentication;

/// <summary>Loads the signed-in user's profile, creating it on their first request.</summary>
internal sealed class ProfileMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IProfileProvisioner profiles)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Validated as a GUID in OnTokenValidated.
            var id = Guid.Parse(context.User.FindFirstValue(SupabaseClaims.Subject)!);
            var profile = await profiles.GetOrCreateAsync(id, DisplayNameFor(context.User), context.RequestAborted);
            context.Features.Set(profile);
        }

        await next(context);
    }

    // Until profiles can be edited: the part of the email before the @.
    private static string DisplayNameFor(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(SupabaseClaims.Email);
        return string.IsNullOrWhiteSpace(email) ? "New user" : email.Split('@')[0];
    }
}

public static class ProfileHttpContextExtensions
{
    /// <summary>The signed-in user's profile. Only available on endpoints that require authentication.</summary>
    public static Profile GetProfile(this HttpContext context) => context.Features.GetRequiredFeature<Profile>();
}
