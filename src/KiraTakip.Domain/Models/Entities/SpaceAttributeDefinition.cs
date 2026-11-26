using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanOzellikTanimlari")]
public class SpaceAttributeDefinition : BaseEntity
{
    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("VeriTipi")]
    public AttributeDataType DataType { get; set; } = AttributeDataType.Text;

    [Column("Secenekler")]
    public string? Options { get; set; }
}
