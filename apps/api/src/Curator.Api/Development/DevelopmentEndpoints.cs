using Curator.Api.Authentication;
using Curator.Infrastructure.Persistence;

namespace Curator.Api.Development;

/// <summary>Endpoints for trying the API locally. Mapped in Development only, never in production.</summary>
public static class DevelopmentEndpoints
{
    public static void MapDevelopmentEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/dev").WithTags("Development");

        // Sample profiles and clips. Some clips are uploaded by the caller.
        dev.MapPost("/sample-data", async (
                HttpContext context,
                CuratorDbContext db,
                TimeProvider time,
                CancellationToken cancellationToken) =>
            {
                var added = await SampleData.SeedAsync(db, context.GetProfile(), time.GetUtcNow(), cancellationToken);
                return Results.Ok(new { added });
            })
            .WithSummary("Adds sample clips. Does nothing if they're already there.");
    }
}
