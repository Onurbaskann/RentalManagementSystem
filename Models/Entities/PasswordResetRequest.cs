using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SifreSifirlamaTalepleri")]
public class PasswordResetRequest : BaseEntity
{
    [Column("UserId")]
    public string UserId { get; set; } = string.Empty;

    [Column("TokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Column("Durum")]
    public PasswordResetStatus Status { get; set; } = PasswordResetStatus.Pending;

    [Column("KullanmaTarihi")]
    public DateTime? UsedAt { get; set; }

    [Column("TalepEdenIp")]
    public string? RequestIp { get; set; }
}
