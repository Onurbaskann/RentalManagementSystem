using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class ApplicationUser : IdentityUser, IAuditable
{
    public string? AdSoyad { get; set; }
    public UserType UserType { get; set; } = UserType.Internal;

    [Column("KiraciId")]
    public int? TenantId { get; set; }
    public bool TumTasinmazlaraErisim { get; set; } = false;
    public bool IsSuperAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
