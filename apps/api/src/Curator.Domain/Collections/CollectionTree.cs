namespace Curator.Domain.Collections;

/// <summary>
/// The shape of the collection tree: each collection's parent. Rules about where a collection may go are checked
/// against it. There are at most a few hundred collections, so the whole tree is loaded.
/// </summary>
public sealed class CollectionTree(IReadOnlyDictionary<Guid, Guid?> parents)
{
    private readonly ILookup<Guid, Guid> children = parents
        .Where(p => p.Value is not null)
        .ToLookup(p => p.Value!.Value, p => p.Key);

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

    /// <summary>
    /// How many levels a collection and its sub-collections take up: 1 without sub-collections, 2 with only direct
    /// ones, and so on.
    /// </summary>
    public int HeightOf(Guid id)
    {
        if (!Contains(id))
        {
            throw new ArgumentException($"Collection {id} isn't in the tree.", nameof(id));
        }

        return HeightOf(id, levels: 1);
    }

    /// <summary>Whether <paramref name="id"/> is <paramref name="ancestorId"/> or one of its sub-collections.</summary>
    public bool IsWithin(Guid id, Guid ancestorId)
    {
        // Throws if the collection is unknown or its path up has a cycle, so the walk below ends.
        DepthOf(id);

        for (Guid? current = id; current is { } c; current = parents[c])
        {
            if (c == ancestorId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many collections a collection is, counting itself and all of its sub-collections.</summary>
    public int CountWithin(Guid id) => IdsWithin(id).Count;

    /// <summary>The IDs of a collection and all of its sub-collections, at every level.</summary>
    public IReadOnlyList<Guid> IdsWithin(Guid id)
    {
        if (!Contains(id))
        {
            throw new ArgumentException($"Collection {id} isn't in the tree.", nameof(id));
        }

        var ids = new List<Guid>();
        AddIdsWithin(id, levels: 1, ids);
        return ids;
    }

    /// <summary>Whether a new collection fits inside <paramref name="parentId"/> without exceeding the depth.</summary>
    public bool CanAddChildTo(Guid parentId) => DepthOf(parentId) < Collection.MaxDepth;

    /// <summary>
    /// Whether <paramref name="id"/>, with its sub-collections, fits inside <paramref name="parentId"/> (or at the top
    /// level, for <c>null</c>) without exceeding the depth. Check <see cref="IsWithin"/> first: moving a collection
    /// into its own sub-collection is never allowed.
    /// </summary>
    public bool CanMove(Guid id, Guid? parentId) =>
        (parentId is { } p ? DepthOf(p) : 0) + HeightOf(id) <= Collection.MaxDepth;

    private int HeightOf(Guid id, int levels)
    {
        ThrowIfCycle(id, levels);
        return 1 + children[id].Select(child => HeightOf(child, levels + 1)).DefaultIfEmpty(0).Max();
    }

    private void AddIdsWithin(Guid id, int levels, List<Guid> ids)
    {
        ThrowIfCycle(id, levels);
        ids.Add(id);
        foreach (var child in children[id])
        {
            AddIdsWithin(child, levels + 1, ids);
        }
    }

    // A cycle can't be created, but if one got into the database, fail instead of recursing forever.
    private void ThrowIfCycle(Guid id, int levels)
    {
        if (levels > parents.Count)
        {
            throw new InvalidOperationException($"The tree has a cycle through collection {id}.");
        }
    }
}
