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

    [Column("KiralanabilirMi")]
    public bool CanBeRented { get; set; } = true;

    [Column("RezervasyonYapilabilirMi")]
    public bool CanBeReserved { get; set; } = false;

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("OlusturmaTarihi")]
    public DateTime CreatedDate { get; set; }

    public ChargeType? ChargeType { get; set; }
}