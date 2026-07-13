using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("GenelTarifeler")]
public class RateSchedule : BaseEntity
{
    [Column("KiraciKategoriId")]
    public int TenantCategoryId { get; set; }

    [Column("Yil")]
    public int Year { get; set; }

    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;

    [Column("BirimDeger")]
    public decimal UnitValue { get; set; }

    [Column("KdvOrani")]
    public decimal KdvRate { get; set; }

    public Category TenantCategory { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
