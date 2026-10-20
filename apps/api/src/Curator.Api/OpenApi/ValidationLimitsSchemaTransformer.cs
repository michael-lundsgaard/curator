using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Writes the limits of <c>[MaxLength]</c>, <c>[Length]</c> and <c>[Range]</c> into the document, so clients know them
/// before a 400. The generator only reads attributes on properties, but use cases put them on record parameters
/// (<c>Command([MaxLength(255)] string Title)</c>), which is where model validation reads them.
/// </summary>
internal sealed class ValidationLimitsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // The record parameter behind a body property, or behind a query parameter ([FromQuery] ListAssets.Query).
        var attributes =
            context.JsonPropertyInfo?.AssociatedParameter?.AttributeProvider?.GetCustomAttributes(inherit: true)
            ?? QueryRecordParameter(context.ParameterDescription)?.GetCustomAttributes(inherit: true)
            ?? [];

        var isArray = schema.Type?.HasFlag(JsonSchemaType.Array) == true;
        foreach (var attribute in attributes.OfType<ValidationAttribute>())
        {
            switch (attribute)
            {
                case MaxLengthAttribute maxLength:
                    SetLength(schema, isArray, min: null, max: maxLength.Length);
                    break;
                case LengthAttribute length:
                    SetLength(schema, isArray, min: length.MinimumLength, max: length.MaximumLength);
                    break;
                case RangeAttribute range:
                    schema.Minimum = Convert.ToString(range.Minimum, CultureInfo.InvariantCulture);
                    schema.Maximum = Convert.ToString(range.Maximum, CultureInfo.InvariantCulture);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static ParameterInfo? QueryRecordParameter(ApiParameterDescription? parameter) =>
        parameter?.ModelMetadata is { ContainerType: { } record, PropertyName: { } name }
            ? record.GetConstructors().SelectMany(c => c.GetParameters()).FirstOrDefault(p => p.Name == name)
            : null;

    private static void SetLength(OpenApiSchema schema, bool isArray, int? min, int max)
    {
        if (isArray)
        {
            schema.MinItems = min ?? schema.MinItems;
            schema.MaxItems = max;
        }
        else
        {
            schema.MinLength = min ?? schema.MinLength;
            schema.MaxLength = max;
        }
    }
}
