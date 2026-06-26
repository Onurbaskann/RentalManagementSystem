namespace KiraTakip.Models.Entities;

public class TahakkukOdeme : BaseEntity
{
    public int TahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public string GirenUserId { get; set; } = string.Empty;
    public string? OnaylayanUserId { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public OdemeKanali OdemeKanali { get; set; }
    public OdemeKaynakTipi OdemeKaynakTipi { get; set; } = OdemeKaynakTipi.Manuel;
    public string? PosReferansNo { get; set; }
    public string? Aciklama { get; set; }
    public OdemeDurumu Durum { get; set; } = OdemeDurumu.OnayBekliyor;
    public DateTime GirisTarihi { get; set; } = DateTime.Now;
    public DateTime? OnayTarihi { get; set; }
    public string? RedNedeni { get; set; }

    public Tahakkuk Tahakkuk { get; set; } = null!;
    public Sozlesme? KiraSozlesmesi { get; set; }
    public ApplicationUser GirenUser { get; set; } = null!;
    public ApplicationUser? OnaylayanUser { get; set; }
    public List<OdemeBankaEslesme> BankaEslesmeleri { get; set; } = [];
}
