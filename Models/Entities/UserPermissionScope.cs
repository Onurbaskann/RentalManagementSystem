using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("KullaniciYetkiKapsamlari")]
public class UserPermissionScope : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    [Column("KapsamTipi")]
    public ScopeType ScopeType { get; set; }

    [Column("KapsamId")]
    public int ScopeId { get; set; }
}
