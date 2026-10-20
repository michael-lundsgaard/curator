using Curator.Api.Authentication;
using Curator.Api.Common;
using Curator.Application.Assets;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Assets;

[ApiController]
[Route("assets")]
public class AssetsController : ControllerBase
{
    /// <summary>List assets</summary>
    /// <remarks>
    /// Lists the shared library, a page at a time. Pass <c>nextCursor</c> as <c>cursor</c> for the next page,
    /// with the same other parameters. Uploads that haven't completed aren't listed. Pass <c>collectionId</c> for
    /// a collection's clips; 404 if the collection doesn't exist.
    /// </remarks>
    [HttpGet(Name = "ListAssets")]
    [ProducesResponseType<ListAssets.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [FromQuery] ListAssets.Query query,
        ListAssets.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Get an asset</summary>
    /// <remarks>
    /// Gets one asset, with signed URLs for playing it and showing its thumbnail. Fetch it again for new URLs
    /// after <c>urlsExpireAt</c>. An upload that hasn't completed is only visible to its uploader.
    /// </remarks>
    [HttpGet("{id:guid}", Name = "GetAsset")]
    [ProducesResponseType<GetAsset.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        GetAsset.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(id), HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Edit an asset</summary>
    /// <remarks>
    /// Edits the title, description and recording time. Send all three: <c>null</c> clears the description or
    /// <c>recordedAt</c>, and a missing one is a 400. Only the uploader and admins can edit an asset.
    /// </remarks>
    [HttpPatch("{id:guid}", Name = "EditAsset")]
    [ProducesResponseType<EditAsset.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(
        Guid id,
        EditAsset.Command command,
        EditAsset.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Delete an asset</summary>
    /// <remarks>
    /// Deletes an asset from the library. Only the uploader and admins can delete it. Deleting it again returns
    /// 404, like any asset that doesn't exist.
    /// </remarks>
    [HttpDelete("{id:guid}", Name = "DeleteAsset")]
    [ProducesResponseType<DeleteAsset.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        DeleteAsset.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(id), HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Start an upload</summary>
    /// <remarks>
    /// Starts an upload. Upload the file to <c>uploadUrl</c> with <c>PUT</c> and the same <c>Content-Type</c>. Storage
    /// rejects a file whose size differs from <c>sizeBytes</c>.
    /// </remarks>
    [HttpPost("uploads", Name = "CreateUpload")]
    [ProducesResponseType<CreateUpload.Response>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUpload(
        CreateUpload.Command command,
        CreateUpload.Handler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(command, HttpContext.GetProfile(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.AssetId }, response);
    }

    /// <summary>Complete an upload</summary>
    /// <remarks>
    /// Completes an upload after the <c>PUT</c> to <c>uploadUrl</c> succeeded. Checks that the file is in storage
    /// with the size the upload was started for. Calling it again is harmless.
    /// </remarks>
    [HttpPost("{id:guid}/complete-upload", Name = "CompleteUpload")]
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
