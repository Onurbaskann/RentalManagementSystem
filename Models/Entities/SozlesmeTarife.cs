using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class SozlesmeTarife : BaseEntity
{
    [Column("LeaseId")]
    public int LeaseId { get; set; }
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }

    public Lease Lease { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
