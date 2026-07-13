using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("OdemeLinkKayitlari")]
public class PaymentLinkRecord : BaseEntity
{
    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("TokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [Column("GecerlilikTarihi")]
    public DateTime ExpiresAt { get; set; }

    [Column("Durum")]
    public PaymentLinkStatus Status { get; set; } = PaymentLinkStatus.Active;

    [Column("IptalEdenUserId")]
    public string? CancelledByUserId { get; set; }

    [Column("IptalTarihi")]
    public DateTime? CancelledAt { get; set; }

    public Tenant? Tenant { get; set; }
}
