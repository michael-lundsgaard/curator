using Curator.Api.Authentication;
using Curator.Domain.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Profiles;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    /// <summary>Get the signed-in user</summary>
    /// <remarks>
    /// Gets the signed-in user's profile. It's created on their first request, named after their email address.
    /// </remarks>
    [HttpGet(Name = "GetMe")]
    public ProfileResponse Get() => ProfileResponse.From(HttpContext.GetProfile());
}

/// <param name="Id">The profile, the same as the Supabase user ID.</param>
/// <param name="DisplayName">The name shown next to the user's clips and collections.</param>
/// <param name="Role">An <c>Admin</c> can edit and delete anything, a <c>Member</c> only what they created.</param>
public sealed record ProfileResponse(Guid Id, string DisplayName, ProfileRole Role)
{
    public static ProfileResponse From(Profile profile) => new(profile.Id, profile.DisplayName, profile.Role);
}
