namespace KiraTakip.Domain.Charges;

public static class ChargeNumberPolicy
{
    private const string Prefix = "THK-";
    private const int MaxSequence = 999999;

    public static string FormatChargeNo(int sequence)
        => $"{Prefix}{sequence:D6}";

    public static bool TryGenerateNextChargeNo(
        IReadOnlySet<string> usedChargeNos,
        out string nextChargeNo,
        int maxAttempts = MaxSequence)
    {
        for (var index = 1; index <= maxAttempts; index++)
        {
            var candidate = FormatChargeNo(index);
            if (!usedChargeNos.Contains(candidate))
            {
                nextChargeNo = candidate;
                return true;
            }
        }

        nextChargeNo = string.Empty;
        return false;
    }
}
