using Curator.Domain.Profiles;

namespace Curator.Application.Profiles;

public interface IProfileProvisioner
{
    /// <summary>Returns the profile of a signed-in user, creating it on their first request.</summary>
    Task<Profile> GetOrCreateAsync(Guid id, string displayName, CancellationToken cancellationToken);
}
