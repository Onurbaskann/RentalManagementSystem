namespace KiraTakip.Models.ViewModels;

public class AylikRaporSatir
{
    public int Ay { get; set; }
    public string AyAdi { get; set; } = string.Empty;
    public int TahakkukSayisi { get; set; }
    public decimal Beklenen { get; set; }
    public decimal TahsilEdilen { get; set; }
    public int GecikmisTahakkukAdet { get; set; }
    public decimal GecikmisTutar { get; set; }
    public double TahsilOrani => Beklenen > 0 ? (double)(TahsilEdilen / Beklenen * 100) : 0;
}
