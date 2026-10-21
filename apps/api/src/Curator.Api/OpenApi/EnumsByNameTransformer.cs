using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Describes enums as the strings they're sent as (<c>JsonStringEnumConverter</c>). The generator leaves out their
/// type, so Scalar shows them empty. It also adds <c>null</c> to an enum used as nullable anywhere, and since every
/// use shares the enum's schema, it would look nullable everywhere. So <c>null</c> is left out, which is only right
/// for an optional query parameter (<c>order</c>): a nullable enum in a body throws, which fails the document tests.
/// </summary>
internal sealed class EnumsByNameTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if ((Nullable.GetUnderlyingType(type) ?? type).IsEnum && schema.Enum is { } values)
        {
            if (context.JsonPropertyInfo is { } property && Nullable.GetUnderlyingType(type) is not null)
            {
                throw new InvalidOperationException(
                    $"{property.DeclaringType.FullName}.{property.Name} is a nullable enum, which the OpenAPI " +
                    "document would show as not nullable. Give it its own schema, or make it not nullable.");
            }

            schema.Type = JsonSchemaType.String;
            schema.Enum = [.. values.Where(value => value is not null)];
        }

        return Task.CompletedTask;
    }
}
