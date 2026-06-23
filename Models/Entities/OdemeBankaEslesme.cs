namespace KiraTakip.Models.Entities;

public class OdemeBankaEslesme : BaseEntity
{
    public int KiraOdemeId { get; set; }
    public int BankaHareketiId { get; set; }
    public string? EslestirenUserId { get; set; }
    public EslesmeTipi EslesmeTipi { get; set; }
    public decimal EslesenTutar { get; set; }
    public DateTime EslesmeTarihi { get; set; } = DateTime.Now;

    public KiraOdeme KiraOdeme { get; set; } = null!;
    public BankaHareketi BankaHareketi { get; set; } = null!;
    public ApplicationUser? EslestirenUser { get; set; }
}
