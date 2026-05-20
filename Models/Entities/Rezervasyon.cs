namespace KiraTakip.Models.Entities;

public class Rezervasyon : BaseEntity
{
    public int BirimId { get; set; }
    public int KiraciId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public int? KiraTahakkukId { get; set; }
    public string OlusturanUserId { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int ToplamSureDakika { get; set; }
    public int UcretsizSureDakika { get; set; }
    public int UcretliSureDakika { get; set; }
    public decimal BirimUcret { get; set; }
    public decimal UcretTutar { get; set; }
    public decimal? KdvOrani { get; set; }
    public decimal? KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public RezervasyonDurumu Durum { get; set; } = RezervasyonDurumu.Planlandi;
    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public Birim Birim { get; set; } = null!;
    public Kiraci Kiraci { get; set; } = null!;
    public KiraSozlesmesi? KiraSozlesmesi { get; set; }
    public KiraTahakkuk? KiraTahakkuk { get; set; }
}
