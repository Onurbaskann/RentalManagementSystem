using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BirimTarifeler")]
public class UnitRate : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

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

    public Unit Unit { get; set; } = null!;
    public Kategori TenantCategory { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
