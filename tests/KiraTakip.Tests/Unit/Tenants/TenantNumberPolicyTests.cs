using KiraTakip.Domain.Tenants;

namespace KiraTakip.Tests;

public class TenantNumberPolicyTests
{
    [Theory]
    [InlineData(1, "KRC-000001")]
    [InlineData(42, "KRC-000042")]
    [InlineData(999999, "KRC-999999")]
    public void FormatTenantNo_ShouldFormatCorrectly(int sequence, string expected)
    {
        var result = TenantNumberPolicy.FormatTenantNo(sequence);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryGenerateNextTenantNo_ShouldReturnFirstAvailableSequence()
    {
        var used = new HashSet<string> { "KRC-000001", "KRC-000002" };

        var success = TenantNumberPolicy.TryGenerateNextTenantNo(used, out var next);

        Assert.True(success);
        Assert.Equal("KRC-000003", next);
    }

    [Fact]
    public void TryGenerateNextTenantNo_ShouldFillGap()
    {
        var used = new HashSet<string> { "KRC-000001", "KRC-000003" };

        var success = TenantNumberPolicy.TryGenerateNextTenantNo(used, out var next);

        Assert.True(success);
        Assert.Equal("KRC-000002", next);
    }

    [Fact]
    public void TryGenerateNextTenantNo_WhenAllExhausted_ShouldReturnFalse()
    {
        var used = new HashSet<string> { "KRC-000001", "KRC-000002" };

        var success = TenantNumberPolicy.TryGenerateNextTenantNo(used, out var next, maxAttempts: 2);

        Assert.False(success);
        Assert.Empty(next);
    }
}
