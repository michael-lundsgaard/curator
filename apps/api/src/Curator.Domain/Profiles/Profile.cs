namespace Curator.Domain.Profiles;

/// <summary>A user of Curator. The ID equals the JWT <c>sub</c> claim issued by Supabase Auth.</summary>
public class Profile
{
    public Profile(Guid id, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
