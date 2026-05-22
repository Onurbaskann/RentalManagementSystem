namespace KiraTakip.Models.Dtos;

public class BirimOzelFiyatRateDto
{
    public int Id { get; set; }
    public string KiraciKategoriAd { get; set; } = string.Empty;
    public string BorcTipiAd { get; set; } = string.Empty;
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}
