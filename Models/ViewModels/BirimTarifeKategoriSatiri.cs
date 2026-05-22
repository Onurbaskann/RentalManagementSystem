namespace KiraTakip.Models.ViewModels;

public class BirimTarifeKategoriSatiri
{
    public int KiraciKategoriId { get; set; }
    public string KiraciKategoriAd { get; set; } = string.Empty;
    public List<BirimTarifeHucre> Hucreler { get; set; } = [];
}
