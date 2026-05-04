namespace KiraTakip.Models;

public class OdemeBankaEslesme
{
    public int Id { get; set; }

    public int KiraOdemeId { get; set; }
    public KiraOdeme KiraOdeme { get; set; } = null!;

    public int BankaHareketiId { get; set; }
    public BankaHareketi BankaHareketi { get; set; } = null!;

    public EslesmeTipi EslesmeTipi { get; set; }
    public string? EslestirenUserId { get; set; }
    public ApplicationUser? EslestirenUser { get; set; }
    public DateTime EslesmeTarihi { get; set; } = DateTime.Now;
}
