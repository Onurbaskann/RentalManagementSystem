using KiraTakip.Data;

namespace KiraTakip.Tests;

public class BranchDatabaseGuardTests
{
    [Theory]
    [InlineData("Server=x;Database=DBTeknoPark_Test;Trusted_Connection=True")]
    [InlineData("Server=x;Initial Catalog=dbteknopark_test;Trusted_Connection=True")]
    public void EnsureAllowed_ShouldPass_ForBranchDatabase(string cs)
    {
        BranchDatabaseGuard.EnsureAllowed(cs);
    }

    [Theory]
    [InlineData("Server=x;Database=KiraTakipDb_Test;Trusted_Connection=True")]
    [InlineData("Server=x;Database=KiraTakipDb;Trusted_Connection=True")]
    [InlineData("Server=x;Trusted_Connection=True")]
    [InlineData("")]
    [InlineData(null)]
    public void EnsureAllowed_ShouldThrow_ForOtherDatabases(string? cs)
    {
        Assert.Throws<InvalidOperationException>(() => BranchDatabaseGuard.EnsureAllowed(cs));
    }
}
