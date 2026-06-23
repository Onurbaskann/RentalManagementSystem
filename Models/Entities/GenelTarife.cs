namespace KiraTakip.Models.Entities;

public class GenelTarife : BaseEntity
{
    public int KiraciKategoriId { get; set; }
    public int Yil { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Kategori KiraciKategori { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
