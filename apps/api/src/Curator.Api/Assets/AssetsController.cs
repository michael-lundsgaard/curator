using Curator.Api.Authentication;
using Curator.Application.Assets;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Assets;

[ApiController]
[Route("assets")]
public class AssetsController : ControllerBase
{
    /// <summary>
    /// Starts an upload. Upload the file to <c>uploadUrl</c> with <c>PUT</c> and the same <c>Content-Type</c>.
    /// </summary>
    [HttpPost("uploads")]
    [ProducesResponseType<CreateUpload.Response>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUpload(
        CreateUpload.Command command,
        CreateUpload.Handler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(command, HttpContext.GetProfile(), cancellationToken);

        // No Location yet: there's no endpoint for a single asset.
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
