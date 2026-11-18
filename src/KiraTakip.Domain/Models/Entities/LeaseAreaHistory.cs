using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeAlanGecmisleri")]
public class LeaseAreaHistory : BaseEntity
{
    [Column("SozlesmeAlanId")]
    public int LeaseAreaId { get; set; }

    [Column("FizikselAlan")]
    public decimal AllocatedArea { get; set; }

    [Column("HesaplamaModu")]
    public LeaseAreaMode Mode { get; set; }

    [Column("GecerlilikBaslangici")]
    public DateTime EffectiveFrom { get; set; }

    [Column("DegisiklikNedeni")]
    public LeaseAreaChangeReason Reason { get; set; }

    public LeaseArea LeaseArea { get; set; } = null!;
}
