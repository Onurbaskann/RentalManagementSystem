namespace KiraTakip.Models;

public class TahakkukKalemi
{
    public int Id { get; set; }

    public int TahakkukId { get; set; }
    public KiraTahakkuk Tahakkuk { get; set; } = null!;

    public int BorcTipiId { get; set; }
    public BorcTipi BorcTipi { get; set; } = null!;

    public string Aciklama { get; set; } = "";
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal Carpan { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }

    public KaynakTipi KaynakTipi { get; set; }
}
