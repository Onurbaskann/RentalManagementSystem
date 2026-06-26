namespace KiraTakip.Models.Entities;

public class OdemeBankaEslesme : BaseEntity
{
    public int TahakkukOdemeId { get; set; }
    public int BankaHareketiId { get; set; }
    public EslesmeTipi EslesmeTipi { get; set; }

    public TahakkukOdeme TahakkukOdeme { get; set; } = null!;
    public BankaHareketi BankaHareketi { get; set; } = null!;
}
