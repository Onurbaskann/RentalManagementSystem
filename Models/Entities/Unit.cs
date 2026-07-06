using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Units")]
public class Unit : BaseEntity
{
    [Column("TasinmazId")]
    public int PropertyId { get; set; }

    [Column("BirimTuruId")]
    public int? UnitTypeId { get; set; }

    [Column("BirimTipi")]
    public UnitKind UnitKind { get; set; }

    [Column("KatNo")]
    public int? FloorNo { get; set; }

    [Column("BirimNo")]
    public string? UnitNo { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Yuzolcumu")]
    public decimal Area { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    public Property Property { get; set; } = null!;
    public UnitType? UnitType { get; set; }
    public List<Lease> Leases { get; set; } = [];
}
