namespace KiraTakip.Models.ViewModels;

public class KiraHesaplamaSonucu
{
    public decimal MevcutKiraBedeli { get; set; }
    public decimal? TufeOrani { get; set; }
    public decimal TufeArtisTutari { get; set; }
    public decimal TufeSonrasiKiraBedeli { get; set; }
    public bool KdvUygulandiMi { get; set; }
    public decimal? KdvRate { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal KdvDahilToplam { get; set; }
}
