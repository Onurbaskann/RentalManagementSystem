namespace KiraTakip.Models.ViewModels;

public class RentIncreaseCalculationResult
{
    public decimal CurrentRentAmount { get; set; }
    public decimal? InflationRate { get; set; }
    public decimal InflationIncreaseAmount { get; set; }
    public decimal RentAfterInflation { get; set; }
    public bool IsVatApplied { get; set; }
    public decimal? VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalIncludingVat { get; set; }
}
