using KiraTakip.Domain.Financial;

namespace KiraTakip.Domain.Leases;

public static class RentIncreasePolicy
{
    public static decimal CalculateInflationAdjustedAmount(decimal currentAmount, decimal inflationRate)
        => currentAmount + (currentAmount * inflationRate / 100);

    public static bool IsValidVatRate(decimal vatRate)
        => VatPolicy.IsValidRate(vatRate);

    public static decimal CalculateVatAmount(decimal amountExcludingVat, decimal vatRate)
        => VatPolicy.CalculateAmount(amountExcludingVat, vatRate);

    public static decimal CalculateVatIncludedAmount(decimal amountExcludingVat, decimal vatRate)
        => VatPolicy.CalculateIncludedAmount(amountExcludingVat, vatRate);

    public static RentIncreaseCalculation Calculate(
        decimal currentRentAmount,
        decimal? inflationRate,
        bool applyVat,
        decimal? vatRate)
    {
        var inflationIncreaseAmount = inflationRate.HasValue
            ? currentRentAmount * inflationRate.Value / 100
            : 0;

        var rentAfterInflation = currentRentAmount + inflationIncreaseAmount;
        var effectiveVatRate = applyVat ? (vatRate ?? 20) : (decimal?)null;
        var vatAmount = applyVat ? VatPolicy.CalculateAmount(rentAfterInflation, effectiveVatRate!.Value) : 0;

        return new RentIncreaseCalculation(
            currentRentAmount,
            inflationRate,
            inflationIncreaseAmount,
            rentAfterInflation,
            applyVat,
            effectiveVatRate,
            vatAmount,
            rentAfterInflation + vatAmount);
    }
}

public sealed record RentIncreaseCalculation(
    decimal CurrentRentAmount,
    decimal? InflationRate,
    decimal InflationIncreaseAmount,
    decimal RentAfterInflation,
    bool IsVatApplied,
    decimal? VatRate,
    decimal VatAmount,
    decimal TotalIncludingVat);
