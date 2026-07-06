namespace KiraTakip.Models.Dtos;

public class TasinmazRezervasyonDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal ToplamTutar { get; set; }
    public ReservationStatus Durum { get; set; }
}
