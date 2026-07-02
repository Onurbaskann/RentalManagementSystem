namespace KiraTakip.Models.Entities;

public class OdemeLinkKayit : BaseEntity
{
    public int KiraciId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public PaymentLinkStatus Durum { get; set; } = PaymentLinkStatus.Active;
    public string? IptalEdenUserId { get; set; }
    public DateTime? IptalTarihi { get; set; }

    public Kiraci? Kiraci { get; set; }
}
