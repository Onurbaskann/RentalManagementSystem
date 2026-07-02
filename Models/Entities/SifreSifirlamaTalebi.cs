namespace KiraTakip.Models.Entities;

public class SifreSifirlamaTalebi : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public PasswordResetStatus Durum { get; set; } = PasswordResetStatus.Pending;
    public DateTime? KullanmaTarihi { get; set; }
    public string? TalepEdenIp { get; set; }
}
