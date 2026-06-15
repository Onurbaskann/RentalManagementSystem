namespace KiraTakip.Models.ViewModels;

public class RezervasyonAlaniDuzenleViewModel
{
    public int? Id { get; set; }
    public string? BirimNo { get; set; }
    public string? Ad { get; set; }
    public decimal Yuzolcumu { get; set; }
    public int? BirimTuruId { get; set; }
    public string? Aciklama { get; set; }
    public int UcretsizSureDakika { get; set; }
    public decimal SaatlikUcret { get; set; }
    public decimal KdvOrani { get; set; } = 20;
    public bool AktifRezervasyonuVar { get; set; }
}
