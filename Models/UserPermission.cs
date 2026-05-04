using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models;

public class UserPermission
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Permission { get; set; } = string.Empty;

    public string? GrantedBy { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
