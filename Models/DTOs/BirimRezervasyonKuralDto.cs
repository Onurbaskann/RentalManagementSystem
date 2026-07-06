namespace KiraTakip.Models.Dtos;

public class BirimRezervasyonKuralDto
{
    public int Id { get; set; }
    public int? BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public decimal PeriyotUcreti { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal KdvRate { get; set; }
}
