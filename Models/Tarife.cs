namespace KiraTakip.Models;

public class TarifeKalemi
{
    public int Id { get; set; }
    public int Yil { get; set; }
    public bool Aktif { get; set; } = true;
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Kategori Kategori { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
