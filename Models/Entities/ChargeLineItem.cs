using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("TahakkukKalemleri")]
public class ChargeLineItem : BaseEntity
{
    [Column("TahakkukId")]
    public int ChargeId { get; set; }

    [Column("TahakkukTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("Aciklama")]
    public string Description { get; set; } = string.Empty;

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; }

    [Column("BirimDegeri")]
    public decimal UnitValue { get; set; }

    [Column("Carpan")]
    public decimal Multiplier { get; set; }

    [Column("Tutar")]
    public decimal Amount { get; set; }

    [Column("KdvOrani")]
    public decimal KdvRate { get; set; }

    [Column("KdvTutari")]
    public decimal KdvAmount { get; set; }

    [Column("ToplamTutar")]
    public decimal TotalAmount { get; set; }

    [Column("KaynakTipi")]
    public LineItemSourceType SourceType { get; set; }

    public Charge Charge { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
