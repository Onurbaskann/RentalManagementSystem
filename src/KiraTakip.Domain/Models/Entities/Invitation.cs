using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Column("TumAlanlaraErisim")]
    public bool HasAccessToAllSpaces { get; set; } = false;

    [Column("AlanIds")]
    public string? SpaceIds { get; set; }

    public Role? Role { get; set; }
}
