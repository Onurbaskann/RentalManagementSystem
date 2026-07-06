using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class TasinmazTarife : BaseEntity
{
    [Column("TasinmazId")]
    public int PropertyId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int ChargeTypeId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }

    public Property Property { get; set; } = null!;
    public Kategori KiraciKategori { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
}
