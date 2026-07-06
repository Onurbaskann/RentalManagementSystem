namespace KiraTakip.Models.Dtos;

public class SozlesmeListItemDto
{
    public int Id { get; set; }
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public string KiraciKategoriAd { get; set; } = string.Empty;
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AylikBedel { get; set; }
    public LeaseStatus Durum { get; set; }
    public decimal BirimYuzolcumu { get; set; }

    public int KalanGun => (int)(EndDate - DateTime.Now).TotalDays;
    public bool Aktif => Durum == LeaseStatus.Active && StartDate <= DateTime.Now && EndDate >= DateTime.Now;
}
