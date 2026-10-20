using Curator.Api.Authentication;
using Curator.Api.Common;
using Curator.Application.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Collections;

[ApiController]
[Route("collections")]
public class CollectionsController : ControllerBase
{
    /// <summary>List collections</summary>
    /// <remarks>
    /// Lists every collection, sorted by name. The tree comes flat: build it from each item's <c>parentId</c>.
    /// </remarks>
    [HttpGet(Name = "ListCollections")]
    [ProducesResponseType<ListCollections.Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        ListCollections.Handler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(new(), cancellationToken));
    }

    /// <summary>Get a collection</summary>
    /// <remarks>
    /// Gets one collection. Its clips are listed by <c>GET /assets</c> with <c>collectionId</c>, and its
    /// sub-collections are the items of <c>GET /collections</c> with this <c>parentId</c>.
    /// </remarks>
    [HttpGet("{id:guid}", Name = "GetCollection")]
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

    /// <summary>Create a collection</summary>
    /// <remarks>
    /// Creates a collection at the top level, or inside <c>parentId</c>. Anyone can create one inside any
    /// collection, up to 5 levels deep.
    /// </remarks>
    [HttpPost(Name = "CreateCollection")]
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

    /// <summary>Edit a collection</summary>
    /// <remarks>
    /// Edits the name and description. Send both: <c>null</c> clears the description, and a missing one is a 400.
    /// Only the creator and admins can edit a collection. Move it with <c>POST /collections/{id}/move</c>.
    /// </remarks>
    [HttpPatch("{id:guid}", Name = "EditCollection")]
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

    /// <summary>Move a collection</summary>
    /// <remarks>
    /// Moves a collection, with its sub-collections, inside <c>parentId</c> or to the top level (<c>null</c>). Only the
    /// creator and admins can move it. Moving it into itself or one of its sub-collections, or deeper than 5 levels
    /// in all, is a conflict.
    /// </remarks>
    [HttpPost("{id:guid}/move", Name = "MoveCollection")]
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

    /// <summary>Delete a collection</summary>
    /// <remarks>
    /// Deletes a collection and all of its sub-collections, also those created by others. Only the creator and
    /// admins can delete it. The clips stay in the library.
    /// </remarks>
    [HttpDelete("{id:guid}", Name = "DeleteCollection")]
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

    /// <summary>Add clips to a collection</summary>
    /// <remarks>
    /// Adds up to 100 clips to a collection. Anyone can add clips to any collection. Clips already in it are skipped,
    /// and a clip that doesn't exist (deleted, or not uploaded yet) fails the whole request.
    /// </remarks>
    [HttpPost("{id:guid}/assets", Name = "AddClipsToCollection")]
    [ProducesResponseType<AddClipsToCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddClips(
        Guid id,
        AddClipsToCollection.Command command,
        AddClipsToCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, HttpContext.GetProfile(), cancellationToken);
        return result.ToActionResult(this, Ok);
    }

    /// <summary>Remove clips from a collection</summary>
    /// <remarks>
    /// Removes up to 100 clips from a collection. Anyone can remove clips from any collection. Clips that aren't in it
    /// are skipped, and the clips stay in the library. A <c>POST</c>, because a body on <c>DELETE</c> is often dropped.
    /// </remarks>
    [HttpPost("{id:guid}/assets/remove", Name = "RemoveClipsFromCollection")]
    [ProducesResponseType<RemoveClipsFromCollection.Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveClips(
        Guid id,
        RemoveClipsFromCollection.Command command,
        RemoveClipsFromCollection.Handler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, command, cancellationToken);
        return result.ToActionResult(this, Ok);
    }
}
