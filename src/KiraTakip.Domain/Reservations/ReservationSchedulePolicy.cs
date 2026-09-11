namespace KiraTakip.Domain.Reservations;

public static class ReservationSchedulePolicy
{
    public static bool HasValidDateRange(DateTime startDate, DateTime endDate)
        => endDate > startDate;

    public static bool MeetsMinimumAdvance(
        DateTime startDate,
        DateTime currentTime,
        int minimumAdvanceMinutes)
        => startDate >= currentTime.AddMinutes(minimumAdvanceMinutes);

    public static bool MeetsMinimumDuration(
        DateTime startDate,
        DateTime endDate,
        int minimumDurationMinutes)
        => (endDate - startDate).TotalMinutes >= minimumDurationMinutes;

    public static bool DoesNotExceedMaximumDuration(
        DateTime startDate,
        DateTime endDate,
        int maximumDurationMinutes)
        => (endDate - startDate).TotalMinutes <= maximumDurationMinutes;

    public static bool IsWithinMaximumAdvance(
        DateTime startDate,
        DateTime currentTime,
        int maximumAdvanceDays)
        => startDate <= currentTime.AddDays(maximumAdvanceDays);

    public static bool HasReachedModificationCutoff(
        DateTime startDate,
        DateTime currentTime,
        int modificationCutoffMinutes)
        => startDate <= currentTime.AddMinutes(modificationCutoffMinutes);

    public static bool HasValidOverrideReason(string? overrideReason, int maximumLength)
        => !string.IsNullOrWhiteSpace(overrideReason)
            && overrideReason.Trim().Length <= maximumLength;

    public static DateTime CalculateCompletionCutoff(DateTime currentTime, int graceMinutes)
        => currentTime.AddMinutes(-graceMinutes);

    public static bool HasReachedCompletionCutoff(DateTime endDate, DateTime cutoff)
        => endDate <= cutoff;
}
