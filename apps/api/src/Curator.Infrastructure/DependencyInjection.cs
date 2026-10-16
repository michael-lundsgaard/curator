using Curator.Application.Persistence;
using Curator.Application.Profiles;
using Curator.Application.Storage;
using Curator.Infrastructure.Persistence;
using Curator.Infrastructure.Profiles;
using Curator.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Curator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolved lazily so tests can override the connection string.
        services.AddDbContext<CuratorDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Curator");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Curator is not configured. Start Postgres with 'docker compose up -d'.");
            }

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddHealthChecks().AddDbContextCheck<CuratorDbContext>("database");

        services.AddScoped<ICuratorDbContext>(sp => sp.GetRequiredService<CuratorDbContext>());

        services.AddOptions<R2Options>().BindConfiguration(R2Options.SectionName);
        services.AddSingleton<IFileStorage, R2FileStorage>();

        services.AddScoped<IProfileProvisioner, ProfileProvisioner>();

        return services;
    }
}
