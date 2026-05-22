namespace KiraTakip.Models.ViewModels;

// GET ViewModel — matris yapısı
public class TarifeMatrisViewModel
{
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisBorcTipiKolon> Kolonlar { get; set; } = [];
    public List<TarifeMatrisSatir> Satirlar { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonSatirlari { get; set; } = [];
}
