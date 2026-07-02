using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class OdemeBankaEslesme : BaseEntity
{
    public int TahakkukOdemeId { get; set; }
    public int BankaHareketiId { get; set; }

    [Column("EslesmeTipi")]
    public MatchType MatchType { get; set; }

    public TahakkukOdeme TahakkukOdeme { get; set; } = null!;
    public BankaHareketi BankaHareketi { get; set; } = null!;
}
