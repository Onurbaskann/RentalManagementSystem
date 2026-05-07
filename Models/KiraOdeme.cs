namespace KiraTakip.Models;

public class KiraOdeme
{
    public int Id { get; set; }

    public int KiraTahakkukId { get; set; }
    public KiraTahakkuk KiraTahakkuk { get; set; } = null!;

    public int? KiraSozlesmesiId { get; set; }
    public KiraSozlesmesi? KiraSozlesmesi { get; set; }

    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public OdemeKanali OdemeKanali { get; set; }
    public string? Aciklama { get; set; }

    public OdemeDurumu Durum { get; set; } = OdemeDurumu.OnayBekliyor;

    public string GirenUserId { get; set; } = string.Empty;
    public ApplicationUser GirenUser { get; set; } = null!;
    public DateTime GirisTarihi { get; set; } = DateTime.Now;

    public string? OnaylayanUserId { get; set; }
    public ApplicationUser? OnaylayanUser { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? RedNedeni { get; set; }

    public List<Dekont> Dekontlar { get; set; } = new();
    public List<OdemeBankaEslesme> BankaEslesmeleri { get; set; } = new();
}
