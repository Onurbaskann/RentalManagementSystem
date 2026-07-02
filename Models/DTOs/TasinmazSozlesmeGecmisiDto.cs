namespace KiraTakip.Models.Dtos;

public class TasinmazSozlesmeGecmisiDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public decimal AylikBedel { get; set; }
    public LeaseStatus Durum { get; set; }
}
