namespace KiraTakip.Domain.Tenants;

public static class TenantNumberPolicy
{
    private const string Prefix = "KRC-";
    private const int MaxSequence = 999999;

    public static string FormatTenantNo(int sequence)
        => $"{Prefix}{sequence:D6}";

    public static bool TryGenerateNextTenantNo(
        IReadOnlySet<string> usedTenantNos,
        out string nextTenantNo,
        int maxAttempts = MaxSequence)
    {
        for (var index = 1; index <= maxAttempts; index++)
        {
            var candidate = FormatTenantNo(index);
            if (!usedTenantNos.Contains(candidate))
            {
                nextTenantNo = candidate;
                return true;
            }
        }

        nextTenantNo = string.Empty;
        return false;
    }
}
