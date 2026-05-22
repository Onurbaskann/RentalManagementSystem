namespace KiraTakip.Models.ViewModels;

public class BirimTarifeHucre
{
    public int RateId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public bool OzelFiyatAktif { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}
