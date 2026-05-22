namespace KiraTakip.Models.ViewModels;

public class BirimDetayViewModel
{
    public Birim Birim { get; set; } = null!;
    public KiraDurumu Durum { get; set; }
    public KiraSozlesmesi? AktifSozlesme { get; set; }
    public decimal AylikBedel { get; set; }
    public RezervasyonTarife? RezKural { get; set; }
}
