using Curator.Domain.Profiles;

namespace Curator.Domain.Collections;

/// <summary>
/// A group of clips in the shared tree, such as "R.E.P.O" or "Best of 2026 → Screams". Everyone can create one,
/// also inside someone else's.
/// </summary>
public class Collection
{
    /// <summary>A top-level collection is at depth 1. Collections can be nested this deep, and no deeper.</summary>
    public const int MaxDepth = 5;

    public Collection(Guid createdBy, Guid? parentId, string name, string? description, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.CreateVersion7(createdAt);
        ParentId = parentId;
        CreatedBy = createdBy;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    /// <summary>The collection this one is inside, or <c>null</c> at the top level.</summary>
    public Guid? ParentId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Its creator and admins may edit, move or delete it. Everyone can add clips and sub-collections to it.
    /// </summary>
    public bool CanBeEditedBy(Profile user) => user.Id == CreatedBy || user.Role == ProfileRole.Admin;

    /// <summary>
    /// Replaces the name and description. The name is required; a blank description is stored as none.
    /// </summary>
    public void Edit(string name, string? description, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
    }

    /// <summary>
    /// Moves it, with its sub-collections, inside <paramref name="parentId"/> or to the top level (<c>null</c>).
    /// Whether it fits there is checked against the <see cref="CollectionTree"/> first.
    /// </summary>
    public void MoveTo(Guid? parentId, DateTimeOffset now)
    {
        if (parentId == Id)
        {
            throw new ArgumentException("A collection can't be inside itself.", nameof(parentId));
        }

        ParentId = parentId;
        UpdatedAt = now;
    }
}
