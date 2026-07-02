namespace KiraTakip.Models.ViewModels;

public class BirimDetayViewModel
{
    public Birim Birim { get; set; } = null!;
    public OccupancyStatus Durum { get; set; }
    public Sozlesme? AktifSozlesme { get; set; }
    public decimal AylikBedel { get; set; }
    public RezervasyonTarife? RezKural { get; set; }
}
