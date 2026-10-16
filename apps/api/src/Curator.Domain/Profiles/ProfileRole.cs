namespace Curator.Domain.Profiles;

public enum ProfileRole
{
    /// <summary>Can add to the library and edit or delete what they created.</summary>
    Member,

    /// <summary>Can edit and delete anything.</summary>
    Admin,
}
