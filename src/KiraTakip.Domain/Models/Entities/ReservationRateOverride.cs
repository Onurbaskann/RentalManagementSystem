using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("RezervasyonTarifeler")]
public class ReservationRateOverride : BaseEntity
{
    [Column("BirimId")]
    public int? UnitId { get; set; }

    [Column("BirimTuruId")]
    public int? UnitTypeId { get; set; }

    [Column("Yil")]
    public int? Year { get; set; }

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

    public Unit? Unit { get; set; }
    public UnitType? UnitType { get; set; }
}
