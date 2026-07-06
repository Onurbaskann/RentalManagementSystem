namespace KiraTakip.Models.Dtos;

public class SozlesmeIslemGecmisiDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public LeaseActivityType IslemTipi { get; set; }
    public string? Aciklama { get; set; }
    public decimal? EskiKiraBedeli { get; set; }
    public decimal? YeniKiraBedeli { get; set; }
    public DateTime? EskiBitisTarihi { get; set; }
    public DateTime? YeniBitisTarihi { get; set; }
    public decimal? TufeOrani { get; set; }
    public bool KdvUygulandiMi { get; set; }
    public decimal? KdvRate { get; set; }
    public decimal? KdvTutari { get; set; }
    public decimal? KdvDahilTutar { get; set; }
}
