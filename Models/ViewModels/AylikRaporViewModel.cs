namespace KiraTakip.Models.ViewModels;

public class AylikRaporViewModel
{
    public int Yil { get; set; }
    public List<AylikRaporSatir> Satirlar { get; set; } = [];
    public decimal ToplamBeklenen => Satirlar.Sum(s => s.Beklenen);
    public decimal ToplamTahsil   => Satirlar.Sum(s => s.TahsilEdilen);
    public int ToplamGecikmiş     => Satirlar.Sum(s => s.GecikmisTahakkukAdet);
    public decimal ToplamGecikmisTutar => Satirlar.Sum(s => s.GecikmisTutar);
    public double GenelTahsilOrani => ToplamBeklenen > 0 ? (double)(ToplamTahsil / ToplamBeklenen * 100) : 0;
}
