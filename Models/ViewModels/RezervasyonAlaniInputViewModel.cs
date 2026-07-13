namespace KiraTakip.Models.ViewModels;

public class RezervasyonAlaniInputViewModel
{
    public string? UnitNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    public int FreeDurationMinutes { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvRate { get; set; } = 20;
}
