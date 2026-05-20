namespace KiraTakip.Models.Entities;

public class SozlesmeTarife : BaseEntity
{
    public int KiraSozlesmesiId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public KiraSozlesmesi KiraSozlesmesi { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
