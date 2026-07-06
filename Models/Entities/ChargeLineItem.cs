using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("ChargeLineItems")]
public class ChargeLineItem : BaseEntity
{
    public int ChargeId { get; set; }

    public int ChargeTypeId { get; set; }

    [Column("Aciklama")]
    public string Description { get; set; } = string.Empty;

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; }

    public decimal UnitValue { get; set; }

    public decimal Multiplier { get; set; }

    public decimal Amount { get; set; }

    public decimal KdvRate { get; set; }

    [Column("KdvTutari")]
    public decimal KdvAmount { get; set; }

    [Column("ToplamTutar")]
    public decimal TotalAmount { get; set; }

    public LineItemSourceType SourceType { get; set; }

    public Charge Charge { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
