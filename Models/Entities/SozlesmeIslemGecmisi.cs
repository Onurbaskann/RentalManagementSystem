namespace KiraTakip.Models.Entities;

public class SozlesmeIslemGecmisi : BaseEntity
{
    public int KiraSozlesmesiId { get; set; }    
    public LeaseActivityType IslemTipi { get; set; }
    public DateTime IslemTarihi { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime? EskiBitisTarihi { get; set; }
    public DateTime? YeniBitisTarihi { get; set; }
    public decimal? EskiKiraBedeli { get; set; }
    public decimal? YeniKiraBedeli { get; set; }
    public decimal? TufeOrani { get; set; }
    public bool? KdvUygulandiMi { get; set; }
    public decimal? KdvOrani { get; set; }
    public decimal? KdvTutari { get; set; }
    public decimal? KdvDahilTutar { get; set; }

    public Sozlesme? KiraSozlesmesi { get; set; }
}
