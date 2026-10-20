using System.Text.Json;
using Curator.Api.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Curator.Api.OpenApi;

public static class OpenApiExtensions
{
    /// <summary>
    /// The OpenAPI document, served by Scalar in Development and written to <c>apps/api/openapi.json</c> on every
    /// build, which the app's API client is generated from.
    /// </summary>
    public static IServiceCollection AddCuratorOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Curator API";
                document.Info.Description =
                    "The shared library of video clips: uploads, collections and search. Every endpoint needs a " +
                    "Supabase access token.";
                return Task.CompletedTask;
            });

            options.CreateSchemaReferenceId = info =>
                (info.Type.DeclaringType is { } useCase ? NestedSchemaId(useCase.Name, info.Type.Name) : null)
                ?? OpenApiOptions.CreateDefaultSchemaReferenceId(info);

            options.AddSchemaTransformer<ValidationLimitsSchemaTransformer>();

            options.AddSchemaTransformer((schema, context, _) =>
            {
                // Enums are sent by name (JsonStringEnumConverter), but the generator leaves out their type, so Scalar
                // shows them empty. It also adds null to an enum used as nullable anywhere, and since every use shares
                // the enum's schema, it would look nullable everywhere. The only nullable use today is the optional
                // order query parameter, so null is left out; a nullable enum in a body would need its own schema.
                if ((Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type).IsEnum
                    && schema.Enum is { } values)
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Enum = [.. values.Where(value => value is not null)];
                }

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, _, _) =>
            {
                // Query parameters bind ignoring case, but are named after the query's properties (Cursor). Show them
                // as clients send them (cursor), like the JSON bodies.
                foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
                {
                    if (parameter.In == ParameterLocation.Query && parameter.Name is { } name)
                    {
                        parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
                    }
                }

                // Errors are ProblemDetails, sent as application/problem+json (RFC 9457).
                foreach (var (status, response) in operation.Responses ?? [])
                {
                    if (status.StartsWith('4') && response.Content is { } content
                        && content.Remove("application/json", out var problem))
                    {
                        content["application/problem+json"] = problem;
                    }
                }

                return Task.CompletedTask;
            });
        });

    /// <summary>The document as JSON and Scalar's reference of it. Development only.</summary>
    public static void MapCuratorApiReference(this IEndpointRouteBuilder app)
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options => options
                .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
                .EnablePersistentAuthentication()
                // Properties in the order the API sends them, not alphabetically or required first.
                .PreserveSchemaPropertyOrder()
                .DisableRequiredPropertiesFirst())
            .AllowAnonymous();
    }

    // Schemas are named by type, and every use case nests a Command, Response or Item (EditAsset.Command), so they
    // would all share one schema. Prefix the use case, and call a command a request, as clients know it:
    // EditAssetRequest, EditAssetResponse, ListAssetsItem. Other nested types (ListAssets.AssetSort) keep their name.
    private static string? NestedSchemaId(string useCase, string type) => type switch
    {
        "Command" => useCase + "Request",
        "Response" or "Item" => useCase + type,
        _ => null,
    };
}
