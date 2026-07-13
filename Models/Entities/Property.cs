using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Tasinmazlar")]
public class Property : BaseEntity
{
    [Column("TasinmazTipiId")]
    public int? PropertyTypeId { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("BirimYapisi")]
    public UnitStructure UnitStructure { get; set; }

    [Column("AcikYuzolcumu")]
    public decimal OpenArea { get; set; }

    [Column("KapaliYuzolcumu")]
    public decimal ClosedArea { get; set; }

    [Column("KatSayisi")]
    public int? FloorCount { get; set; }

    [Column("Il")]
    public string City { get; set; } = string.Empty;

    [Column("Ilce")]
    public string District { get; set; } = string.Empty;

    [Column("Mahalle")]
    public string Neighborhood { get; set; } = string.Empty;

    [Column("AcikAdres")]
    public string Address { get; set; } = string.Empty;

    [Column("Aciklama")]
    public string? Description { get; set; }

    public PropertyType? PropertyType { get; set; }
    public List<Unit> Units { get; set; } = [];
}
