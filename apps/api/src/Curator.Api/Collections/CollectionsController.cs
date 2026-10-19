using Curator.Api.Authentication;
using Curator.Api.Common;
using Curator.Application.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Collections;

[ApiController]
[Route("collections")]
public class CollectionsController : ControllerBase
{
    /// <summary>
    /// Lists every collection, sorted by name. The tree comes flat: build it from each item's <c>parentId</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ListCollections.Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        ListCollections.Handler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new(), cancellationToken));
    }

    /// <summary>Gets one collection.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        GetCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(id), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>
    /// Creates a collection at the top level, or inside <c>parentId</c>. Anyone can create one inside any
    /// collection, up to 5 levels deep.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CreateCollection.Response>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateCollection.Command command,
        CreateCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(Get), new { id = response.Id }, response));
    }

    /// <summary>
    /// Edits the name and description. Send both: a missing or <c>null</c> description clears it. Only the creator
    /// and admins can edit a collection. Move it with <c>POST /collections/{id}/move</c>.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<EditCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(
        Guid id,
        EditCollection.Command command,
        EditCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>
    /// Moves a collection, with its sub-collections, inside <c>parentId</c> or to the top level (<c>null</c>). Only the
    /// creator and admins can move it. Moving it into itself or one of its sub-collections, or deeper than 5 levels
    /// in all, is a conflict.
    /// </summary>
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType<MoveCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Move(
        Guid id,
        MoveCollection.Command command,
        MoveCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>
    /// Deletes a collection and all of its sub-collections, also those created by others. Only the creator and
    /// admins can delete it. The clips stay in the library.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<DeleteCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        DeleteCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(id), HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }
}
