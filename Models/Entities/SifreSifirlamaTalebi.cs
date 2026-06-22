namespace KiraTakip.Models.Entities;

public class SifreSifirlamaTalebi : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public SifreSifirlamaDurum Durum { get; set; } = SifreSifirlamaDurum.Beklemede;
    public DateTime? KullanmaTarihi { get; set; }
    public string? TalepEdenIp { get; set; }
}
