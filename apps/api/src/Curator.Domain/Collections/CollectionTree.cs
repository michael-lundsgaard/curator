namespace Curator.Domain.Collections;

/// <summary>
/// The shape of the collection tree: each collection's parent. Rules about where a collection may go are checked
/// against it. There are at most a few hundred collections, so the whole tree is loaded.
/// </summary>
public sealed class CollectionTree(IReadOnlyDictionary<Guid, Guid?> parents)
{
    public bool Contains(Guid id) => parents.ContainsKey(id);

    /// <summary>
    /// How far down the tree a collection is, counted from the top: 1 for a top-level collection (no parent), 2 for
    /// one inside it, and so on. Found by walking up through the parents.
    /// </summary>
    public int DepthOf(Guid id)
    {
        var depth = 0;
        Guid? current = id;
        while (current is { } c)
        {
            if (!parents.TryGetValue(c, out var parent))
            {
                throw new ArgumentException($"Collection {c} isn't in the tree.", nameof(id));
            }

            // A cycle can't be created, but if one got into the database, fail instead of looping forever.
            if (++depth > parents.Count)
            {
                throw new InvalidOperationException($"The tree has a cycle through collection {id}.");
            }

            current = parent;
        }

        return depth;
    }

    /// <summary>Whether a new collection fits inside <paramref name="parentId"/> without exceeding the depth.</summary>
    public bool CanAddChildTo(Guid parentId) => DepthOf(parentId) < Collection.MaxDepth;
}
