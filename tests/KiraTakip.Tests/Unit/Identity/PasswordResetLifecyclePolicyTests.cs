using KiraTakip.Domain.Identity;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class PasswordResetLifecyclePolicyTests
{
    [Fact]
    public void Validate_WhenStatusIsUsed_ReturnsUsed()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddHours(1);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Used, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Used, result);
    }

    [Fact]
    public void Validate_WhenStatusIsUsedAndExpired_ReturnsUsedDueToPrecedence()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddMinutes(-10);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Used, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Used, result);
    }

    [Fact]
    public void Validate_WhenStatusIsCancelled_ReturnsCancelled()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddHours(1);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Cancelled, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Cancelled, result);
    }

    [Fact]
    public void Validate_WhenStatusIsCancelledAndExpired_ReturnsCancelledDueToPrecedence()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddMinutes(-10);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Cancelled, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Cancelled, result);
    }

    [Fact]
    public void Validate_WhenExpiresAtIsPast_ReturnsExpired()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddSeconds(-1);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Pending, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Expired, result);
    }

    [Fact]
    public void Validate_WhenExpiresAtEqualsNow_ReturnsValid()
    {
        // Policy: expiresAt < utcNow is Expired. Exactly equal is still Valid.
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now;

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Pending, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Valid, result);
    }

    [Fact]
    public void Validate_WhenExpiresAtIsInFutureAndPending_ReturnsValid()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddHours(1);

        var result = PasswordResetLifecyclePolicy.Validate(PasswordResetStatus.Pending, expiresAt, now);

        Assert.Equal(PasswordResetValidationResult.Valid, result);
    }

    [Theory]
    [InlineData(0, 3, false)]
    [InlineData(2, 3, false)]
    [InlineData(3, 3, true)]
    [InlineData(4, 3, true)]
    public void IsRateLimitExceeded_ValidatesBoundariesCorrectly(int recentCount, int maxRequests, bool expected)
    {
        var result = PasswordResetLifecyclePolicy.IsRateLimitExceeded(recentCount, maxRequests);

        Assert.Equal(expected, result);
    }
}
