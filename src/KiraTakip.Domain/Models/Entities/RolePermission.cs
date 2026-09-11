using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("RolYetkileri")]
public class RolePermission
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("RolId")]
    public int RoleId { get; set; }

    [Column("Permission")]
    public string Permission { get; set; } = string.Empty;

    public Role? Role { get; set; }
}
