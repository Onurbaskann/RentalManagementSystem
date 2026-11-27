using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("OdemeMagazaYonlendirmeleri")]
public class PaymentStoreRouting : BaseEntity
{
    [Column("BorcTipiId")]
    public int ChargeTypeId { get; set; }

    [Column("AlanId")]
    public int? SpaceId { get; set; }

    [Column("MagazaId")]
    public int StoreId { get; set; }

    public ChargeType ChargeType { get; set; } = null!;
    public Space? Space { get; set; }
    public Store Store { get; set; } = null!;
}
