namespace KiraTakip.Models.DTOs;

public class TahakkukKalemiPreview
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior Davranis { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal Multiplier { get; set; }
    public decimal Amount { get; set; }
    public decimal KdvRate { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public LineItemSourceType SourceType { get; set; }
    public bool RateBulundu { get; set; }
    public string? Aciklama { get; set; }
}
