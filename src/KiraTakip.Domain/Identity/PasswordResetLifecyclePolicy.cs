using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Identity;

public enum PasswordResetValidationResult
{
    Valid = 0,
    Used = 1,
    Cancelled = 2,
    Expired = 3
}

public static class PasswordResetLifecyclePolicy
{
    public static PasswordResetValidationResult Validate(PasswordResetStatus status, DateTime expiresAt, DateTime utcNow)
    {
        if (status == PasswordResetStatus.Used)
            return PasswordResetValidationResult.Used;

        if (status == PasswordResetStatus.Cancelled)
            return PasswordResetValidationResult.Cancelled;

        if (expiresAt < utcNow)
            return PasswordResetValidationResult.Expired;

        return PasswordResetValidationResult.Valid;
    }

    public static bool IsRateLimitExceeded(int recentCount, int maxRequests)
        => recentCount >= maxRequests;
}
