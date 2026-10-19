using Curator.Application.Assets;
using Curator.Application.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Curator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<CreateUpload.Handler>();
        services.AddScoped<CompleteUpload.Handler>();
        services.AddScoped<ListAssets.Handler>();
        services.AddScoped<GetAsset.Handler>();
        services.AddScoped<EditAsset.Handler>();
        services.AddScoped<DeleteAsset.Handler>();

        services.AddScoped<CreateCollection.Handler>();
        services.AddScoped<GetCollection.Handler>();

        return services;
    }
}
