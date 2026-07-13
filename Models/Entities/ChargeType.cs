using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BorcTipleri")]
public class ChargeType : BaseEntity
{
    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("Davranis")]
    public ChargeTypeBehavior Behavior { get; set; } = ChargeTypeBehavior.MonthlyFixed;

    [Column("Sistem")]
    public bool IsSystem { get; set; } = false;
}
