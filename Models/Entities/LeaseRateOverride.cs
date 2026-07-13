using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeTarifeler")]
public class LeaseRateOverride : BaseEntity
{
    [Column("LeaseId")]
    public int LeaseId { get; set; }

    [Column("ChargeTypeId")]
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;

    [Column("UnitValue")]
    public decimal UnitValue { get; set; }

    [Column("KdvRate")]
    public decimal KdvRate { get; set; }

    public Lease Lease { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
