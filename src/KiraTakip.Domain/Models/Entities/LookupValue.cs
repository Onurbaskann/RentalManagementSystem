using System.ComponentModel.DataAnnotations.Schema;
using KiraTakip.Domain.Auditing;

namespace KiraTakip.Models.Entities;

[Table("EnumDegerleri")]
[AuditExclude]
public class LookupValue : BaseEntity
{
    [Column("EnumAdi")]
    public string EnumName { get; set; } = null!;

    [Column("Deger")]
    public int Value { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = null!;

    [Column("Aciklama")]
    public string? Description { get; set; }
}
