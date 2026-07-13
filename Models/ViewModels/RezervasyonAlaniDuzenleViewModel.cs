namespace KiraTakip.Models.ViewModels;

public class RezervasyonAlaniDuzenleViewModel
{
    public int? Id { get; set; }
    public string? UnitNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvRate { get; set; } = 20;
    public bool AktifRezervasyonuVar { get; set; }
}
