using KiraTakip.Domain.Spaces;

namespace KiraTakip.Tests;

public class SpaceTreePolicyTests
{
    [Theory]
    [InlineData(null, 1, "/1/")]
    [InlineData("", 1, "/1/")]
    [InlineData("/1/", 5, "/1/5/")]
    [InlineData("/1/5/", 12, "/1/5/12/")]
    public void BuildPath_ShouldAppendId(string? parentPath, int id, string expected)
        => Assert.Equal(expected, SpaceTreePolicy.BuildPath(parentPath, id));

    [Fact]
    public void GetAncestorIds_ShouldExcludeSelf_AndKeepRootFirst()
        => Assert.Equal([1, 5], SpaceTreePolicy.GetAncestorIds("/1/5/12/"));

    [Fact]
    public void GetAncestorIds_ShouldBeEmpty_ForRoot()
        => Assert.Empty(SpaceTreePolicy.GetAncestorIds("/1/"));

    [Fact]
    public void IsAncestorOrSelf_ShouldBeTrue_ForSelfAndDescendant()
    {
        Assert.True(SpaceTreePolicy.IsAncestorOrSelf("/1/5/", "/1/5/"));
        Assert.True(SpaceTreePolicy.IsAncestorOrSelf("/1/5/", "/1/5/12/"));
    }

    [Fact]
    public void IsAncestorOrSelf_ShouldDistinguish_5From50()
    {
        Assert.False(SpaceTreePolicy.IsAncestorOrSelf("/1/5/", "/1/50/"));
        Assert.False(SpaceTreePolicy.IsAncestorOrSelf("/1/5/", "/1/50/7/"));
    }

    [Fact]
    public void IsAncestorOrSelf_ShouldBeFalse_ForParentOfAncestor()
        => Assert.False(SpaceTreePolicy.IsAncestorOrSelf("/1/5/12/", "/1/5/"));

    [Theory]
    [InlineData(0, true)]
    [InlineData(18, true)]
    [InlineData(19, false)]
    [InlineData(20, false)]
    public void CanAddChild_ShouldRespectMaxDepth(int parentDepth, bool expected)
        => Assert.Equal(expected, SpaceTreePolicy.CanAddChild(parentDepth));

    [Fact]
    public void MaxDepth_ShouldBe19()
        => Assert.Equal(19, SpaceTreePolicy.MaxDepth);

    [Fact]
    public void FindNestedPairs_ShouldReturnAncestorDescendantPairs()
    {
        var spaces = new[] { (1, "/1/"), (5, "/1/5/"), (12, "/1/5/12/"), (7, "/7/") };

        var pairs = SpaceTreePolicy.FindNestedPairs(spaces);

        Assert.Equal(3, pairs.Count);
        Assert.Contains((1, 5), pairs);
        Assert.Contains((1, 12), pairs);
        Assert.Contains((5, 12), pairs);
    }

    [Fact]
    public void FindNestedPairs_ShouldBeEmpty_ForSiblingsAndSimilarPrefixes()
    {
        var spaces = new[] { (5, "/1/5/"), (50, "/1/50/"), (6, "/1/6/") };

        Assert.Empty(SpaceTreePolicy.FindNestedPairs(spaces));
    }

    [Fact]
    public void FindNestedPairs_ShouldIgnoreDuplicateSameId()
        => Assert.Empty(SpaceTreePolicy.FindNestedPairs([(5, "/1/5/"), (5, "/1/5/")]));

    [Theory]
    [InlineData("/1/", 1)]
    [InlineData("/42/5/12/", 42)]
    public void GetRootId_ShouldReturnFirstSegment(string path, int expected)
        => Assert.Equal(expected, SpaceTreePolicy.GetRootId(path));

    [Fact]
    public void GetRootId_ShouldThrow_ForEmptyPath()
        => Assert.Throws<ArgumentException>(() => SpaceTreePolicy.GetRootId("/"));
}
