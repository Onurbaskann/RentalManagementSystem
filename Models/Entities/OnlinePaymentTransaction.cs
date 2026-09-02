using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SanalPosIslemleri")]
public class OnlinePaymentTransaction : BaseEntity
{
    [Column("TahakkukKalemiId")]
    public int ChargeLineItemId { get; set; }

    [Column("MagazaHesapBilgisiId")]
    public int StoreAccountId { get; set; }

    [Column("BaslatanKullaniciId")]
    public string InitiatedByUserId { get; set; } = string.Empty;

    [Column("OdemeId")]
    public int? PaymentAllocationId { get; set; }

    [Column("SaglayiciKodu")]
    public string ProviderCode { get; set; } = string.Empty;

    [Column("UyeIsyeriOdemeNo")]
    public string MerchantPaymentId { get; set; } = string.Empty;

    [Column("SaglayiciIslemNo")]
    public string? ProviderTransactionId { get; set; }

    [Column("Tutar")]
    public decimal Amount { get; set; }

    [Column("ParaBirimi")]
    public string Currency { get; set; } = string.Empty;

    [Column("Durum")]
    public OnlinePaymentTransactionStatus Status { get; set; } = OnlinePaymentTransactionStatus.Pending;

    [Column("YanitKodu")]
    public string? ResponseCode { get; set; }

    [Column("IslemDurumu")]
    public string? TransactionStatus { get; set; }

    [Column("HataKodu")]
    public string? ErrorCode { get; set; }

    [Column("GuvenliMesaj")]
    public string? SafeMessage { get; set; }

    [Column("OturumSonTarihi")]
    public DateTime? SessionExpiresAt { get; set; }

    [Column("GeriBildirimAlinmaTarihi")]
    public DateTime? CallbackReceivedAt { get; set; }

    [Column("SonSorgulamaTarihi")]
    public DateTime? LastInquiryAt { get; set; }

    [Column("SorgulamaSayisi")]
    public int InquiryCount { get; set; }

    [Column("TamamlanmaTarihi")]
    public DateTime? CompletedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ChargeLineItem ChargeLineItem { get; set; } = null!;
    public StoreAccount StoreAccount { get; set; } = null!;
    public ApplicationUser InitiatedByUser { get; set; } = null!;
    public PaymentAllocation? PaymentAllocation { get; set; }
    public List<OnlinePaymentEvent> Events { get; set; } = [];
}
