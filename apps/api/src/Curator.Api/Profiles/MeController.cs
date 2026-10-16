using Curator.Api.Authentication;
using Curator.Domain.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Profiles;

[ApiController]
[Route("me")]
public class MeController : ControllerBase
{
    /// <summary>The signed-in user's profile.</summary>
    [HttpGet]
    public ProfileResponse Get() => ProfileResponse.From(HttpContext.GetProfile());
}

public sealed record ProfileResponse(Guid Id, string DisplayName, ProfileRole Role)
{
    public static ProfileResponse From(Profile profile) => new(profile.Id, profile.DisplayName, profile.Role);
}
