using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Identity;

public enum InvitationValidationResult
{
    Valid = 0,
    Cancelled = 1,
    Accepted = 2,
    Expired = 3
}

public enum InvitationResendEligibility
{
    Eligible = 0,
    Accepted = 1,
    Cancelled = 2
}

public readonly record struct InvitationCooldownCheckResult(
    bool IsOnCooldown,
    int RemainingMinutes);

public static class InvitationLifecyclePolicy
{
    public static InvitationValidationResult Validate(InvitationStatus status, DateTime expiresAt, DateTime utcNow)
    {
        if (status == InvitationStatus.Cancelled)
            return InvitationValidationResult.Cancelled;

        if (status == InvitationStatus.Accepted)
            return InvitationValidationResult.Accepted;

        if (expiresAt < utcNow)
            return InvitationValidationResult.Expired;

        return InvitationValidationResult.Valid;
    }

    public static bool CanCancel(InvitationStatus status)
        => status == InvitationStatus.Pending;

    public static InvitationResendEligibility CheckResendEligibility(InvitationStatus status)
    {
        if (status == InvitationStatus.Accepted)
            return InvitationResendEligibility.Accepted;

        if (status == InvitationStatus.Cancelled)
            return InvitationResendEligibility.Cancelled;

        return InvitationResendEligibility.Eligible;
    }

    public static InvitationCooldownCheckResult CheckResendCooldown(
        DateTime lastSentAt,
        int cooldownMinutes,
        DateTime utcNow)
    {
        var resendCooldown = TimeSpan.FromMinutes(cooldownMinutes);
        var elapsed = utcNow - lastSentAt;
        var remainingMinutes = (int)(resendCooldown - elapsed).TotalMinutes;

        return new InvitationCooldownCheckResult(
            IsOnCooldown: remainingMinutes > 0,
            RemainingMinutes: remainingMinutes);
    }
}
