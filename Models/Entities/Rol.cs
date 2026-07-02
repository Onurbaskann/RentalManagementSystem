using KiraTakip.Models;

namespace KiraTakip.Models.Entities;

public class Rol : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public RoleScope Scope { get; set; }
    public int? KiraciId { get; set; }
    public bool IsSystemRole { get; set; }

    public ICollection<RolPermission> RolPermissions { get; set; } = new List<RolPermission>();
    public ICollection<UserRol> UserRoller { get; set; } = new List<UserRol>();
}
