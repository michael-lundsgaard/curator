using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace Curator.Api.OpenApi;

/// <summary>
/// Names the schemas of a use case's nested types after the use case. Schemas are named by type, and every use case
/// nests a <c>Command</c>, <c>Response</c> or <c>Item</c> (<c>EditAsset.Command</c>), so they would all share one
/// schema. A command is called a request, as clients know it: <c>EditAssetRequest</c>, <c>EditAssetResponse</c>,
/// <c>ListAssetsItem</c>. Other nested types (<c>ListAssets.AssetSort</c>) keep their name.
/// </summary>
internal static class UseCaseSchemaIds
{
    public static string? Create(JsonTypeInfo info) =>
        (info.Type.DeclaringType is { } useCase ? NestedSchemaId(useCase.Name, info.Type.Name) : null)
        ?? OpenApiOptions.CreateDefaultSchemaReferenceId(info);

    private static string? NestedSchemaId(string useCase, string type) => type switch
    {
        "Command" => useCase + "Request",
        "Response" or "Item" => useCase + type,
        _ => null,
    };
}
