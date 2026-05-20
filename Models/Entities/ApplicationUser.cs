using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Models.Entities;

public class ApplicationUser : IdentityUser, IAuditable
{
    public string? AdSoyad { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
