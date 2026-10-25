using Curator.Application.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Profiles;

[ApiController]
[Route("profiles")]
public class ProfilesController : ControllerBase
{
    /// <summary>List profiles</summary>
    /// <remarks>
    /// Lists everyone who has signed in, sorted by display name. Filter clips by one with <c>uploadedBy</c> on
    /// <c>GET /assets</c>.
    /// </remarks>
    [HttpGet(Name = "ListProfiles")]
    [ProducesResponseType<ListProfiles.Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        ListProfiles.Handler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new(), cancellationToken));
    }
}
