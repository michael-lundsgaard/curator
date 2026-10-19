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
}
