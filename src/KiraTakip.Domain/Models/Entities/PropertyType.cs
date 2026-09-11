using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("TasinmazTipleri")]
public class PropertyType : BaseEntity
{
    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Sira")]
    public int SortOrder { get; set; }

    [Column("TekBirimDestekli")]
    public bool SupportsSingleUnit { get; set; }

    [Column("CokluBirimDestekli")]
    public bool SupportsMultipleUnits { get; set; }
}
