using Curator.Api.Authentication;
using Curator.Api.Common;
using Curator.Application.Assets;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Assets;

[ApiController]
[Route("assets")]
public class AssetsController : ControllerBase
{
    /// <summary>
    /// Starts an upload. Upload the file to <c>uploadUrl</c> with <c>PUT</c> and the same <c>Content-Type</c>. Storage
    /// rejects a file whose size differs from <c>sizeBytes</c>.
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

    /// <summary>
    /// Completes an upload after the <c>PUT</c> to <c>uploadUrl</c> succeeded. Checks that the file is in storage
    /// with the size the upload was started for. Calling it again is harmless.
    /// </summary>
    [HttpPost("{id:guid}/complete-upload")]
    [ProducesResponseType<CompleteUpload.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteUpload(
        Guid id,
        CompleteUpload.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(id), HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }
}
