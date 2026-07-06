namespace KiraTakip.Models.Dtos;

public class TasinmazSozlesmeGecmisiDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AylikBedel { get; set; }
    public LeaseStatus Durum { get; set; }
}
