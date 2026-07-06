namespace KiraTakip.Models.ViewModels;

public class BirimDetayViewModel
{
    public Unit Unit { get; set; } = null!;
    public OccupancyStatus Durum { get; set; }
    public Lease? AktifSozlesme { get; set; }
    public decimal AylikBedel { get; set; }
    public RezervasyonTarife? RezKural { get; set; }
}
