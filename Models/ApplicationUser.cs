using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Models;

public class ApplicationUser : IdentityUser
{
    public string? AdSoyad { get; set; }
    public bool IsActive { get; set; } = true;
}
