namespace KiraTakip.Models.Dtos;

public class SozlesmeKalemInputDto
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior Davranis { get; set; }
    public decimal VarsayilanTutar { get; set; }
    public decimal Amount { get; set; }
    public decimal UnitValue { get; set; }
    public decimal VarsayilanBirimDeger { get; set; }
    public decimal KdvRate { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public bool KullaniciDegistirdiMi { get; set; }
    public bool RateBulundu { get; set; }
    public string? SourceType { get; set; }
}
