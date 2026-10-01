namespace KiraTakip.Domain.Leases;

public static class LeaseNumberPolicy
{
    private const string Prefix = "SZL-";
    private const int MaxSequence = 999999;

    public static string FormatLeaseNo(int sequence)
        => $"{Prefix}{sequence:D6}";

    public static bool TryGenerateNextLeaseNo(
        IReadOnlySet<string> usedLeaseNos,
        out string nextLeaseNo,
        int maxAttempts = MaxSequence)
    {
        for (var index = 1; index <= maxAttempts; index++)
        {
            var candidate = FormatLeaseNo(index);
            if (!usedLeaseNos.Contains(candidate))
            {
                nextLeaseNo = candidate;
                return true;
            }
        }

        nextLeaseNo = string.Empty;
        return false;
    }
}
