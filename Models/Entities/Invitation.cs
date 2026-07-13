using System.ComponentModel.DataAnnotations.Schema;
using KiraTakip.Models;

namespace KiraTakip.Models.Entities;

[Table("Davetiyeler")]
public class Invitation : BaseEntity
{
    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Column("AdSoyad")]
    public string? FullName { get; set; }

    [Column("KullaniciTipi")]
    public UserType UserType { get; set; } = UserType.Internal;

    [Column("KiraciId")]
    public int? TenantId { get; set; }

    [Column("RolId")]
    public int RoleId { get; set; }

    [Column("TokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("GecerlilikTarihi")]
    public DateTime ExpiresAt { get; set; }

    [Column("Durum")]
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    [Column("DavetEdenKullaniciId")]
    public string InvitedByUserId { get; set; } = string.Empty;

    [Column("KabulTarihi")]
    public DateTime? AcceptedAt { get; set; }

    [Column("OlusanKullaniciId")]
    public string? CreatedUserId { get; set; }

    [Column("TumTasinmazlaraErisim")]
    public bool HasAccessToAllProperties { get; set; } = false;

    [Column("TasinmazIds")]
    public string? PropertyIds { get; set; }

    [Column("BirimIds")]
    public string? UnitIds { get; set; }

    public Role? Role { get; set; }
}
