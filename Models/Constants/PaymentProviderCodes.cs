namespace KiraTakip.Models.Constants;

public static class PaymentProviderCodes
{
    public const string Paratika = "Paratika";

    public static readonly IReadOnlyList<string> Supported = [Paratika];
}

public static class CurrencyCodes
{
    public const string Try = "TRY";

    public static readonly IReadOnlyList<string> Supported = [Try];
}
