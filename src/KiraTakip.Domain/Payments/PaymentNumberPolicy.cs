namespace KiraTakip.Domain.Payments;

public static class PaymentNumberPolicy
{
    private const string Prefix = "ODM-";
    private const int MaxSequence = 999999;

    public static string FormatPaymentNo(int sequence)
        => $"{Prefix}{sequence:D6}";

    public static bool TryGenerateNextPaymentNo(
        IReadOnlySet<string> usedPaymentNos,
        out string nextPaymentNo,
        int maxAttempts = MaxSequence)
    {
        for (var index = 1; index <= maxAttempts; index++)
        {
            var candidate = FormatPaymentNo(index);
            if (!usedPaymentNos.Contains(candidate))
            {
                nextPaymentNo = candidate;
                return true;
            }
        }

        nextPaymentNo = string.Empty;
        return false;
    }
}
