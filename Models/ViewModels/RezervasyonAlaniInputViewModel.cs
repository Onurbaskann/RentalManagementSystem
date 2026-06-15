namespace KiraTakip.Models.ViewModels;

public class RezervasyonAlaniInputViewModel
{
    public string? BirimNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public int? BirimTuruId { get; set; }
    public string? Aciklama { get; set; }
    public int UcretsizSureDakika { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvOrani { get; set; } = 20;
}
