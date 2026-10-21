using Curator.Api.Authentication;
using Microsoft.AspNetCore.OpenApi;
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

    // Each transformer fixes one thing the generator gets wrong; its comment says what and why (ADR 0008).
    private static void Configure(OpenApiOptions options, string title, string description)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = title;
            document.Info.Description = description;
            return Task.CompletedTask;
        });

        options.CreateSchemaReferenceId = UseCaseSchemaIds.Create;

        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddDocumentTransformer<NoServersTransformer>();
        options.AddDocumentTransformer<OneLineDescriptionsTransformer>();
        options.AddSchemaTransformer<ValidationLimitsSchemaTransformer>();
        options.AddSchemaTransformer<EnumsByNameTransformer>();
        options.AddOperationTransformer<CamelCaseQueryParametersTransformer>();
        options.AddOperationTransformer<ProblemJsonErrorsTransformer>();
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
}
