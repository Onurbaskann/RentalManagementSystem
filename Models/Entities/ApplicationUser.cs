using KiraTakip.Models;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Models.Entities;

public class ApplicationUser : IdentityUser, IAuditable
{
    public string? AdSoyad { get; set; }
    public bool IsActive { get; set; } = true;
    public UserType UserType { get; set; } = UserType.Internal;
    public int? KiraciId { get; set; }
    public bool TumTasinmazlaraErisim { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
