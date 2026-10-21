using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Leaves out <c>servers</c>. The generator lists the host the document was requested from, which in the committed
/// file would be the test host (<c>http://localhost/</c>). Scalar uses its own address instead, and the app has its
/// own.
/// </summary>
internal sealed class NoServersTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Servers = [];
        return Task.CompletedTask;
    }
}
