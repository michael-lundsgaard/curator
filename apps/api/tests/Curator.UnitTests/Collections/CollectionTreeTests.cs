using Curator.Domain.Collections;

namespace Curator.UnitTests.Collections;

public class CollectionTreeTests
{
    /// <summary>A chain of <paramref name="length"/> collections, each inside the previous one.</summary>
    private static (CollectionTree Tree, Guid[] Chain) CreateChain(int length)
    {
        var chain = NewChain(length);
        return (CreateTree(chain), chain);
    }

    private static Guid[] NewChain(int length) => Enumerable.Range(0, length).Select(_ => Guid.NewGuid()).ToArray();

    /// <summary>A tree of separate chains, each starting at the top level.</summary>
    private static CollectionTree CreateTree(params Guid[][] chains) =>
        new(chains
            .SelectMany(chain => chain.Select((id, i) => (id, parent: i == 0 ? (Guid?)null : chain[i - 1])))
            .ToDictionary(x => x.id, x => x.parent));

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
    public void HeightOf_CountsTheLevelsBelowAndTheCollectionItself()
    {
        var (tree, chain) = CreateChain(3);

        Assert.Equal(3, tree.HeightOf(chain[0]));
        Assert.Equal(1, tree.HeightOf(chain[2]));
    }

    [Fact]
    public void HeightOf_TakesTheDeepestBranch()
    {
        Guid root = Guid.NewGuid(), leaf = Guid.NewGuid(), branch = Guid.NewGuid(), twig = Guid.NewGuid();
        var tree = new CollectionTree(new Dictionary<Guid, Guid?>
        {
            [root] = null,
            [leaf] = root,
            [branch] = root,
            [twig] = branch,
        });

        Assert.Equal(3, tree.HeightOf(root));
        Assert.Equal(4, tree.CountWithin(root));
        Assert.Equal(2, tree.CountWithin(branch));
    }

    [Fact]
    public void IsWithin_TheCollectionAndItsSubCollections()
    {
        var (tree, chain) = CreateChain(3);

        Assert.True(tree.IsWithin(chain[0], chain[0]));
        Assert.True(tree.IsWithin(chain[2], chain[0]));
        Assert.False(tree.IsWithin(chain[0], chain[2]));
    }

    [Fact]
    public void CanMove_CountsTheDepthOfTheTargetAndTheHeightOfTheMovedSubtree()
    {
        var target = NewChain(Collection.MaxDepth);
        var moved = NewChain(3);
        var tree = CreateTree(target, moved);

        // Inside depth 2: 2 + height 3 = 5 fits. Inside depth 3 it doesn't.
        Assert.True(tree.CanMove(moved[0], target[1]));
        Assert.False(tree.CanMove(moved[0], target[2]));
        Assert.True(tree.CanMove(moved[1], null));
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
