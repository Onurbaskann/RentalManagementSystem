namespace KiraTakip.Models.ViewModels;

// POST ViewModel — düz liste
public class TarifeMatrisPostViewModel
{
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisHucre> Hucreler { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonHucreler { get; set; } = [];
}
