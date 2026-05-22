namespace KiraTakip.Models.ViewModels;

public class TarifeMatrisSatir
{
    public int KiraciKategoriId { get; set; }
    public string KiraciKategoriAd { get; set; } = string.Empty;
    public List<TarifeMatrisHucre> Hucreler { get; set; } = [];
}
