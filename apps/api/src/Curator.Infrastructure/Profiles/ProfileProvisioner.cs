using Curator.Application.Profiles;
using Curator.Domain.Profiles;
using Curator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Curator.Infrastructure.Profiles;

internal sealed class ProfileProvisioner(CuratorDbContext db, TimeProvider time) : IProfileProvisioner
{
    public async Task<Profile> GetOrCreateAsync(Guid id, string displayName, CancellationToken cancellationToken)
    {
        var existing = await db.Profiles.FindAsync([id], cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var profile = new Profile(id, displayName, time.GetUtcNow());
        db.Profiles.Add(profile);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A parallel first request (e.g. the app loading several things at once) created it first.
            db.Entry(profile).State = EntityState.Detached;
            return await db.Profiles.SingleAsync(p => p.Id == id, cancellationToken);
        }
    }
}
