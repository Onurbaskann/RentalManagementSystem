using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Magazalar")]
public class Store : BaseEntity
{
    [Column("Kod")]
    public string Code { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Aciklama")]
    public string? Description { get; set; }

    public ICollection<StoreAccount> Accounts { get; set; } = [];
}
