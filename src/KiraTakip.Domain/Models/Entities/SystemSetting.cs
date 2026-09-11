using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SistemAyarlari")]
public class SystemSetting : BaseEntity
{
    [Column("Anahtar")]
    public string Key { get; set; } = string.Empty;

    [Column("Deger")]
    public string Value { get; set; } = string.Empty;
}
