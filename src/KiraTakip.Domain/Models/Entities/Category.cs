using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Kategoriler")]
public class Category : BaseEntity
{
    [Column("Tipi")]
    public CategoryType Type { get; set; }

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Sira")]
    public int Order { get; set; }

}

public enum CategoryType
{
    Tenant = 1,
    Sector = 2
}
