using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Curator.Api.Common;

public static class ControllerExtensions
{
    /// <summary>Controllers that read and write JSON only, described the same way in the OpenAPI document.</summary>
    public static IServiceCollection AddCuratorControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                // JSON only, so the OpenAPI document doesn't list each body as text/plain and text/json too.
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.InputFormatters.OfType<SystemTextJsonInputFormatter>().Single()
                    .SupportedMediaTypes.Remove("text/json");
                options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Single()
                    .SupportedMediaTypes.Remove("text/json");
            })
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

        // The OpenAPI document reads these options, not the controllers' above, so it describes the same JSON.
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());

        // The web defaults also accept numbers in quotes ("5"), which the OpenAPI document then types as integer or
        // string.
        options.NumberHandling = JsonNumberHandling.Strict;
    }
}
