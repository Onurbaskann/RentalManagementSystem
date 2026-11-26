using KiraTakip.Models.Dtos;

namespace KiraTakip.Tests;

public class SpaceAccessScopeTests
{
    [Fact]
    public void Global_IncludesEverything()
        => Assert.True(new SpaceAccessScope(true, []).Includes("/9/"));

    [Fact]
    public void SingleRoot_IncludesSelfAndDescendants()
    {
        var scope = new SpaceAccessScope(false, ["/1/2/"]);
        Assert.True(scope.Includes("/1/2/"));
        Assert.True(scope.Includes("/1/2/7/"));
        Assert.False(scope.Includes("/1/"));
    }

    [Fact]
    public void MultipleRoots_IncludeAny()
    {
        var scope = new SpaceAccessScope(false, ["/1/2/", "/5/"]);
        Assert.True(scope.Includes("/5/6/"));
        Assert.True(scope.Includes("/1/2/3/"));
        Assert.False(scope.Includes("/1/3/"));
    }

    [Fact]
    public void NoFalsePositive_OnNumericPrefix()
    {
        var scope = new SpaceAccessScope(false, ["/1/2/"]);
        Assert.False(scope.Includes("/1/20/"));
        Assert.False(scope.Includes("/1/20/3/"));
    }

    [Fact]
    public void EmptyScope_IncludesNothing()
        => Assert.False(new SpaceAccessScope(false, []).Includes("/1/"));
}
