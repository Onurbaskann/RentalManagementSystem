namespace KiraTakip.Models.ViewModels;

public class RezervasyonAlaniDuzenleViewModel
{
    public int? Id { get; set; }
    public string? BirimNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Aciklama { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvRate { get; set; } = 20;
    public bool AktifRezervasyonuVar { get; set; }
}
