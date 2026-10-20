using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Joins the lines of every summary and description with a space. They come from XML comments wrapped at 120 columns,
/// not from intended paragraphs, and the compiler ends those lines with the OS's line break, so without this
/// <c>openapi.json</c> would change depending on which OS built it.
/// </summary>
internal sealed partial class OneLineDescriptionsTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var operation in document.Paths.Values.SelectMany(
                     path => path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>()))
        {
            operation.Summary = OneLine(operation.Summary);
            operation.Description = OneLine(operation.Description);

            foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
            {
                parameter.Description = OneLine(parameter.Description);
            }

            if (operation.RequestBody is OpenApiRequestBody body)
            {
                body.Description = OneLine(body.Description);
            }

            foreach (var response in operation.Responses?.Values.OfType<OpenApiResponse>() ?? [])
            {
                response.Description = OneLine(response.Description);
            }
        }

        foreach (var schema in document.Components?.Schemas?.Values ?? [])
        {
            JoinLines(schema);
        }

        return Task.CompletedTask;
    }

    private static void JoinLines(IOpenApiSchema? schema)
    {
        switch (schema)
        {
            case OpenApiSchema concrete:
                concrete.Description = OneLine(concrete.Description);
                foreach (var property in concrete.Properties?.Values ?? [])
                {
                    JoinLines(property);
                }

                JoinLines(concrete.Items);
                break;

            // A property's own description next to a $ref (Status). Reading it falls back to the referenced schema's,
            // so it's only set when it changes; the referenced schema is joined as a component.
            case OpenApiSchemaReference reference when OneLine(reference.Description) is var line
                && line != reference.Description:
                reference.Description = line;
                break;
        }
    }

    // The generator trims the indentation of most comment lines, but not of a description next to a $ref.
    private static string? OneLine(string? text) => text is null ? null : LineBreak().Replace(text, " ");

    [GeneratedRegex(@"\s*[\r\n]\s*")]
    private static partial Regex LineBreak();
}
