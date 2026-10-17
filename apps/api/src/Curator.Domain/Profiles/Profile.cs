namespace Curator.Domain.Profiles;

/// <summary>A user of Curator. The ID equals the JWT <c>sub</c> claim issued by Supabase Auth.</summary>
public class Profile
{
    public Profile(Guid id, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        Role = ProfileRole.Member;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public ProfileRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>No endpoint calls this yet. Roles are changed with SQL, see ADR 0003.</summary>
    public void ChangeRole(ProfileRole role) => Role = role;
}
