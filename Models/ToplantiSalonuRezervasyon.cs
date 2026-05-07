namespace KiraTakip.Models;

public class ToplantiSalonuRezervasyon
{
    public int Id { get; set; }

    public int BirimId { get; set; }
    public Birim Birim { get; set; } = null!;

    public int KiraciId { get; set; }
    public Kiraci Kiraci { get; set; } = null!;

    public int? KiraSozlesmesiId { get; set; }
    public KiraSozlesmesi? KiraSozlesmesi { get; set; }

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

    public int? KiraTahakkukId { get; set; }
    public KiraTahakkuk? KiraTahakkuk { get; set; }

    public string? Aciklama { get; set; }

    public string OlusturanUserId { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; }
}
