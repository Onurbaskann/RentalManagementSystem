namespace KiraTakip.Models.Dtos;

public class SozlesmeTarifeDto
{
    public int Id { get; set; }
    public int BorcTipiId { get; set; }
    public string BorcTipiKod { get; set; } = string.Empty;
    public string BorcTipiAd { get; set; } = string.Empty;
    public ChargeTypeBehavior BorcTipiDavranis { get; set; }
    public decimal BirimDeger { get; set; }
    public CalculationMethod CalculationMethod { get; set; }
    public decimal KdvOrani { get; set; }
}
