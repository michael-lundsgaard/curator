using System.Text.Json;
using Curator.Api.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Curator.Api.OpenApi;

public static class OpenApiExtensions
{
    /// <summary>The OpenAPI document of the Development endpoints (<c>/dev</c>), apart from the public one.</summary>
    public const string DevDocument = "dev";

    /// <summary>
    /// The OpenAPI documents, served by Scalar in Development. The public one (<c>v1</c>) is committed as
    /// <c>apps/api/openapi.json</c>, which the app's API client is generated from; <c>OpenApiDocumentTests</c> keeps
    /// it up to date. Endpoints without a group name go in <c>v1</c> only, those with <see cref="DevDocument"/> in the
    /// dev document only.
    /// </summary>
    public static IServiceCollection AddCuratorOpenApi(this IServiceCollection services) =>
        services
            .AddOpenApi(options => Configure(
                options,
                "Curator API",
                "The shared library of video clips: uploads, collections and search. Every endpoint needs a Supabase " +
                "access token."))
            .AddOpenApi(DevDocument, options =>
            {
                Configure(
                    options,
                    "Curator development tools",
                    "Endpoints for trying the API locally, in Development only. Every endpoint needs a Supabase " +
                    "access token.");

                // By default, endpoints without a group name are in every document.
                options.ShouldInclude = description => description.GroupName == DevDocument;
            });

    private static void Configure(OpenApiOptions options, string title, string description)
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = title;
            document.Info.Description = description;

            // The generator lists the host the document was requested from, which in the committed file would be the
            // test host (http://localhost/). Scalar uses its own address instead, and the app has its own.
            document.Servers = [];
            return Task.CompletedTask;
        });

        options.AddDocumentTransformer<OneLineDescriptionsTransformer>();

        options.CreateSchemaReferenceId = info =>
            (info.Type.DeclaringType is { } useCase ? NestedSchemaId(useCase.Name, info.Type.Name) : null)
            ?? OpenApiOptions.CreateDefaultSchemaReferenceId(info);

        options.AddSchemaTransformer<ValidationLimitsSchemaTransformer>();

        options.AddSchemaTransformer((schema, context, _) =>
        {
            // Enums are sent by name (JsonStringEnumConverter), but the generator leaves out their type, so Scalar
            // shows them empty. It also adds null to an enum used as nullable anywhere, and since every use shares
            // the enum's schema, it would look nullable everywhere. So null is left out, which is only right for
            // an optional query parameter (order): a nullable enum in a body throws, which fails the document tests.
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
    }

    /// <summary>The documents as JSON and Scalar's reference of them. Development only.</summary>
    public static void MapCuratorApiReference(this IEndpointRouteBuilder app)
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference(options => options
                .AddDocument("v1", "Curator API", isDefault: true)
                .AddDocument(DevDocument, "Development tools")
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
