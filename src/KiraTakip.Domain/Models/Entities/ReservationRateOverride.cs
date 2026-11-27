using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("RezervasyonTarifeler")]
public class ReservationRateOverride : BaseEntity
{
    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("UcretsizSureDakika")]
    public int FreeDurationMinutes { get; set; }

    [Column("UcretlendirmePeriyoduDakika")]
    public int BillingPeriodMinutes { get; set; }

    [Column("PeriyotUcreti")]
    public decimal PeriodRate { get; set; }

    [Column("KdvOrani")]
    public decimal KdvRate { get; set; } = 20;

    [Column("Aciklama")]
    public string? Description { get; set; }

    public Space Space { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
