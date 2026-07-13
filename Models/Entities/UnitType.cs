using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BirimTurleri")]
public class UnitType : BaseEntity
{
    [Column("BorcTipiId")]
    public int? ChargeTypeId { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("KullanimTuru")]
    public UnitTypeUsage Usage { get; set; } = UnitTypeUsage.Rentable;

    [Column("Sira")]
    public int SortOrder { get; set; }

    public ChargeType? ChargeType { get; set; }
}