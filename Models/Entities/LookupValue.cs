using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("EnumDegerleri")]
public class LookupValue : BaseEntity
{
    [Column("EnumName")]
    public string EnumName { get; set; } = null!;

    [Column("Value")]
    public int Value { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = null!;

    [Column("Aciklama")]
    public string? Description { get; set; }
}
