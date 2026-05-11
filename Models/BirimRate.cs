namespace KiraTakip.Models;

public class BirimRate
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Birim Birim { get; set; } = null!;
    public KiraciKategori KiraciKategori { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
