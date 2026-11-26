using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanOzellikDegerleri")]
public class SpaceAttributeValue : BaseEntity
{
    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("OzellikTanimId")]
    public int AttributeDefinitionId { get; set; }

    [Column("Deger")]
    public string Value { get; set; } = string.Empty;

    public Space Space { get; set; } = null!;
    public SpaceAttributeDefinition AttributeDefinition { get; set; } = null!;
}
