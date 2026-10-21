using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Lists errors as <c>application/problem+json</c> (RFC 9457), which is what the API sends them as. The generator
/// lists them as <c>application/json</c>.
/// </summary>
internal sealed class ProblemJsonErrorsTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var (status, response) in operation.Responses ?? [])
        {
            if (status.StartsWith('4') && response.Content is { } content
                && content.Remove("application/json", out var problem))
            {
                content["application/problem+json"] = problem;
            }
        }

        return Task.CompletedTask;
    }
}
