namespace KiraTakip.Models;

public class SozlesmeRate
{
    public int Id { get; set; }
    public int KiraSozlesmesiId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public KiraSozlesmesi KiraSozlesmesi { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
