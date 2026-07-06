namespace KiraTakip.Models.Dtos;

public class RezervasyonListItemDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public int? ChargeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int PaidDurationMinutes { get; set; }
    public decimal ToplamTutar { get; set; }
    public ReservationStatus Durum { get; set; }
    public string? Aciklama { get; set; }
}
