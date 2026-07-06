namespace KiraTakip.Models.Dtos;

public class TahakkukKalemDto
{
    public string ChargeTypeCode { get; set; } = string.Empty;
    public int BorcTipiSira { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal Multiplier { get; set; }
    public decimal Amount { get; set; }
    public decimal KdvRate { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public LineItemSourceType SourceType { get; set; }
}
