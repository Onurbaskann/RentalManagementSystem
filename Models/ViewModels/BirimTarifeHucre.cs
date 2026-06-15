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

    // Fallback/Varsayılan değer bilgileri
    public decimal VarsayilanBirimDeger { get; set; }
    public decimal VarsayilanKdvOrani { get; set; }
    public HesaplamaYontemi VarsayilanHesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public string VarsayilanKaynak { get; set; } = string.Empty;
}
