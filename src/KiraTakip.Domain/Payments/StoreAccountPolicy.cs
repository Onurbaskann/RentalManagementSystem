using KiraTakip.Models.Constants;

namespace KiraTakip.Domain.Payments;

public static class StoreAccountPolicy
{
    public static bool IsProviderSupported(string providerCode)
        => PaymentProviderCodes.Supported.Contains(
            providerCode,
            StringComparer.OrdinalIgnoreCase);

    public static bool IsCurrencySupported(string currency)
        => CurrencyCodes.Supported.Contains(
            currency,
            StringComparer.OrdinalIgnoreCase);

    public static bool HasActiveAccount(int activeAccountCount)
        => activeAccountCount > 0;

    public static bool HasAccountConflict(int activeAccountCount)
        => activeAccountCount > 1;
}
