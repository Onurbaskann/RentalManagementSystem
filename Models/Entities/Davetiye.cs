using KiraTakip.Models;

namespace KiraTakip.Models.Entities;

public class Davetiye : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? AdSoyad { get; set; }
    public UserType UserType { get; set; } = UserType.Internal;
    public int? KiraciId { get; set; }
    public int RolId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Durum { get; set; } = InvitationStatus.Pending;
    public string DavetEdenUserId { get; set; } = string.Empty;
    public DateTime? KabulTarihi { get; set; }
    public string? OlusanUserId { get; set; }
    public bool TumTasinmazlaraErisim { get; set; } = false;
    public string? TasinmazIds { get; set; }
    public string? BirimIds { get; set; }

    public Rol? Rol { get; set; }
}
