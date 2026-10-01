namespace KiraTakip.Domain.Reservations;

public static class ReservationNumberPolicy
{
    private const string Prefix = "RZV-";
    private const int MaxSequence = 999999;

    public static string FormatReservationNo(int sequence)
        => $"{Prefix}{sequence:D6}";

    public static bool TryGenerateNextReservationNo(
        IReadOnlySet<string> usedReservationNos,
        out string nextReservationNo,
        int maxAttempts = MaxSequence)
    {
        for (var index = 1; index <= maxAttempts; index++)
        {
            var candidate = FormatReservationNo(index);
            if (!usedReservationNos.Contains(candidate))
            {
                nextReservationNo = candidate;
                return true;
            }
        }

        nextReservationNo = string.Empty;
        return false;
    }
}
