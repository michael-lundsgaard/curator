using Curator.Application.Assets;
using Curator.Application.Collections;
using Curator.Application.Profiles;
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
        services.AddScoped<ListCollections.Handler>();
        services.AddScoped<EditCollection.Handler>();
        services.AddScoped<MoveCollection.Handler>();
        services.AddScoped<DeleteCollection.Handler>();
        services.AddScoped<AddClipsToCollection.Handler>();
        services.AddScoped<RemoveClipsFromCollection.Handler>();

        services.AddScoped<ListProfiles.Handler>();

        return services;
    }
}
