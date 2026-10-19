using Curator.Domain.Collections;

namespace Curator.UnitTests.Collections;

public class CollectionTreeTests
{
    /// <summary>A chain of <paramref name="length"/> collections, each inside the previous one.</summary>
    private static (CollectionTree Tree, Guid[] Chain) CreateChain(int length)
    {
        var chain = Enumerable.Range(0, length).Select(_ => Guid.NewGuid()).ToArray();
        var parents = chain.Select((id, i) => (id, parent: i == 0 ? (Guid?)null : chain[i - 1]))
            .ToDictionary(x => x.id, x => x.parent);
        return (new CollectionTree(parents), chain);
    }

    [Fact]
    public void DepthOf_CountsTheCollectionAndItsAncestors()
    {
        var (tree, chain) = CreateChain(3);

        Assert.Equal(1, tree.DepthOf(chain[0]));
        Assert.Equal(3, tree.DepthOf(chain[2]));
    }

    [Fact]
    public void CanAddChildTo_AllowsUpToMaxDepth()
    {
        var (tree, chain) = CreateChain(Collection.MaxDepth);

        Assert.True(tree.CanAddChildTo(chain[^2]));
        Assert.False(tree.CanAddChildTo(chain[^1]));
    }

    [Fact]
    public void DepthOf_UnknownCollection_Throws()
    {
        var (tree, _) = CreateChain(1);

        Assert.Throws<ArgumentException>(() => tree.DepthOf(Guid.NewGuid()));
    }

    [Fact]
    public void DepthOf_WithCycle_ThrowsInsteadOfLooping()
    {
        Guid a = Guid.NewGuid(), b = Guid.NewGuid();
        var tree = new CollectionTree(new Dictionary<Guid, Guid?> { [a] = b, [b] = a });

        Assert.Throws<InvalidOperationException>(() => tree.DepthOf(a));
    }
}
