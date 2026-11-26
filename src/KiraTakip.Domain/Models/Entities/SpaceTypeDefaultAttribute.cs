using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanTuruVarsayilanOzellikleri")]
public class SpaceTypeDefaultAttribute : BaseEntity
{
    [Column("AlanTuruId")]
    public int SpaceTypeId { get; set; }

    [Column("OzellikTanimId")]
    public int AttributeDefinitionId { get; set; }

    public SpaceType SpaceType { get; set; } = null!;
    public SpaceAttributeDefinition AttributeDefinition { get; set; } = null!;
}
