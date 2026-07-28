using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeTarifeler")]
public class LeaseRateOverride : BaseEntity
{
    [Column("SozlesmeId")]
    public int LeaseId { get; set; }

    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;

    [Column("BirimDeger")]
    public decimal UnitValue { get; set; }

    [Column("KdvOrani")]
    public decimal KdvRate { get; set; }

    public Lease Lease { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
