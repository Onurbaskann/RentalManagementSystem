namespace KiraTakip.Models;

public class SozlesmeRate
{
    public int Id { get; set; }
    public int SozlesmeId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public KiraSozlesmesi Sozlesme { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
