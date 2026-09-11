namespace KiraTakip.Domain.Financial;

public static class VatPolicy
{
    public static bool IsValidRate(decimal vatRate)
        => vatRate is >= 0 and <= 100;

    public static decimal CalculateAmount(decimal amountExcludingVat, decimal vatRate)
        => Math.Round(amountExcludingVat * vatRate / 100m, 2, MidpointRounding.ToEven);

    public static decimal CalculateIncludedAmount(decimal amountExcludingVat, decimal vatRate)
        => amountExcludingVat + CalculateAmount(amountExcludingVat, vatRate);
}
