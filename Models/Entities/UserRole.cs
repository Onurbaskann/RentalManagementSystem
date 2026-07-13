using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("KullaniciRoller")]
public class UserRole : BaseEntity
{
    [Column("UserId")]
    public string UserId { get; set; } = string.Empty;

    [Column("RolId")]
    public int RoleId { get; set; }

    public Role? Role { get; set; }
}
