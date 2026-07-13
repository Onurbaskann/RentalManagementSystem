using KiraTakip.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Roller")]
public class Role : BaseEntity
{
    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Scope")]
    public RoleScope Scope { get; set; }

    [Column("KiraciId")]
    public int? TenantId { get; set; }

    [Column("IsSystemRole")]
    public bool IsSystemRole { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
