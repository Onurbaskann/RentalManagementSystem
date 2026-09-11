using KiraTakip.Domain.Identity;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class InvitationLifecyclePolicyTests
{
    [Fact]
    public void Validate_WhenStatusIsCancelled_ReturnsCancelled()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddDays(1);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Cancelled, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Cancelled, result);
    }

    [Fact]
    public void Validate_WhenStatusIsCancelledAndExpired_ReturnsCancelledDueToPrecedence()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddMinutes(-5);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Cancelled, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Cancelled, result);
    }

    [Fact]
    public void Validate_WhenStatusIsAccepted_ReturnsAccepted()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddDays(1);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Accepted, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Accepted, result);
    }

    [Fact]
    public void Validate_WhenStatusIsAcceptedAndExpired_ReturnsAcceptedDueToPrecedence()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddMinutes(-5);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Accepted, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Accepted, result);
    }

    [Fact]
    public void Validate_WhenExpiresAtIsPast_ReturnsExpired()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddSeconds(-1);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Pending, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Expired, result);
    }

    [Fact]
    public void Validate_WhenExpiresAtIsFutureAndPending_ReturnsValid()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var expiresAt = now.AddMinutes(30);

        var result = InvitationLifecyclePolicy.Validate(InvitationStatus.Pending, expiresAt, now);

        Assert.Equal(InvitationValidationResult.Valid, result);
    }

    [Theory]
    [InlineData(InvitationStatus.Pending, true)]
    [InlineData(InvitationStatus.Accepted, false)]
    [InlineData(InvitationStatus.Cancelled, false)]
    [InlineData(InvitationStatus.Expired, false)]
    public void CanCancel_OnlyPendingCanBeCancelled(InvitationStatus status, bool expected)
    {
        var result = InvitationLifecyclePolicy.CanCancel(status);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(InvitationStatus.Accepted, InvitationResendEligibility.Accepted)]
    [InlineData(InvitationStatus.Cancelled, InvitationResendEligibility.Cancelled)]
    [InlineData(InvitationStatus.Pending, InvitationResendEligibility.Eligible)]
    [InlineData(InvitationStatus.Expired, InvitationResendEligibility.Eligible)]
    public void CheckResendEligibility_ValidatesStatusCorrectly(InvitationStatus status, InvitationResendEligibility expected)
    {
        var result = InvitationLifecyclePolicy.CheckResendEligibility(status);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CheckResendCooldown_WhenTimeElapsedIsLessThanCooldown_ReturnsOnCooldownWithRemainingMinutes()
    {
        var now = new DateTime(2026, 9, 8, 12, 10, 0, DateTimeKind.Utc);
        var lastSentAt = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        const int cooldownMinutes = 15;

        var result = InvitationLifecyclePolicy.CheckResendCooldown(lastSentAt, cooldownMinutes, now);

        Assert.True(result.IsOnCooldown);
        Assert.Equal(5, result.RemainingMinutes);
    }

    [Fact]
    public void CheckResendCooldown_WhenTimeElapsedEqualsCooldown_ReturnsNotOnCooldown()
    {
        var now = new DateTime(2026, 9, 8, 12, 15, 0, DateTimeKind.Utc);
        var lastSentAt = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        const int cooldownMinutes = 15;

        var result = InvitationLifecyclePolicy.CheckResendCooldown(lastSentAt, cooldownMinutes, now);

        Assert.False(result.IsOnCooldown);
        Assert.Equal(0, result.RemainingMinutes);
    }

    [Fact]
    public void CheckResendCooldown_WhenTimeElapsedExceedsCooldown_ReturnsNotOnCooldownWithNegativeRemaining()
    {
        var now = new DateTime(2026, 9, 8, 12, 25, 0, DateTimeKind.Utc);
        var lastSentAt = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        const int cooldownMinutes = 15;

        var result = InvitationLifecyclePolicy.CheckResendCooldown(lastSentAt, cooldownMinutes, now);

        Assert.False(result.IsOnCooldown);
        Assert.True(result.RemainingMinutes < 0);
    }
}
