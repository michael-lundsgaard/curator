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

        // Assets and collections from manual testing pile up. Profiles stay, so the caller keeps their role.
        dev.MapDelete("/data", async (CuratorDbContext db, CancellationToken cancellationToken) =>
            {
                if (!SampleData.IsLocalDatabase(db))
                {
                    return Results.Problem(
                        "Only a database on this machine can be cleared.", statusCode: StatusCodes.Status403Forbidden);
                }

                var deleted = await SampleData.ClearAsync(db, cancellationToken);
                return Results.Ok(new { deleted });
            })
            .WithSummary("Deletes all assets, collections and the sample profiles. Files in storage are left alone.");
    }
}
