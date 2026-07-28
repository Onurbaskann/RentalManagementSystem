using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("TasinmazTarifeler")]
public class PropertyRateOverride : BaseEntity
{
    [Column("TasinmazId")]
    public int PropertyId { get; set; }

    [Column("KiraciKategoriId")]
    public int TenantCategoryId { get; set; }

    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;

    [Column("BirimDeger")]
    public decimal UnitValue { get; set; }

    [Column("KdvOrani")]
    public decimal KdvRate { get; set; }

    public Property Property { get; set; } = null!;
    public Category TenantCategory { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
