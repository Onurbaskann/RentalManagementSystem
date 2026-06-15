namespace KiraTakip.Models.Dtos;

public class RezervasyonListItemDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public string TasinmazAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public int? KiraSozlesmesiId { get; set; }
    public int? KiraTahakkukId { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int ToplamSureDakika { get; set; }
    public int UcretsizSureDakika { get; set; }
    public int UcretliSureDakika { get; set; }
    public decimal ToplamTutar { get; set; }
    public RezervasyonDurumu Durum { get; set; }
    public string? Aciklama { get; set; }
}
