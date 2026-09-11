using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("OdemeMagazaYonlendirmeleri")]
public class PaymentStoreRouting : BaseEntity
{
    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("TasinmazId")]
    public int? PropertyId { get; set; }

    [Column("BirimId")]
    public int? UnitId { get; set; }

    [Column("MagazaId")]
    public int StoreId { get; set; }

    public ChargeType ChargeType { get; set; } = null!;
    public Property? Property { get; set; }
    public Unit? Unit { get; set; }
    public Store Store { get; set; } = null!;
}
