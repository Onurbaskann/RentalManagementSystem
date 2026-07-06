namespace KiraTakip.Models.Dtos;

public class RateValueDto
{
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}
