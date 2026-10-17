using Curator.Application.Assets;
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

        return services;
    }
}
