namespace KiraTakip.Models.ViewModels;

public class TarifeDetayViewModel
{
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeKalemSatiri> Kalemler { get; set; } = [];
}
