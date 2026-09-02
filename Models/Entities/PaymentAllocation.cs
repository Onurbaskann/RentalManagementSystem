using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("TahakkukOdemeleri")]
public class PaymentAllocation : BaseEntity
{
    [Column("TahakkukId")]
    public int ChargeId { get; set; }

    [Column("TahakkukKalemiId")]
    public int ChargeLineItemId { get; set; }

    [Column("MagazaHesapBilgisiId")]
    public int StoreAccountId { get; set; }

    [Column("SozlesmeId")]
    public int? LeaseId { get; set; }

    [Column("GirenKullaniciId")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("OnaylayanKullaniciId")]
    public string? ApprovedByUserId { get; set; }

    [Column("OdemeTarihi")]
    public DateTime PaymentDate { get; set; }

    [Column("Tutar")]
    public decimal Amount { get; set; }

    [Column("OdemeKanali")]
    public PaymentChannel PaymentChannel { get; set; }

    [Column("OdemeKaynakTipi")]
    public PaymentSourceType PaymentSourceType { get; set; } = PaymentSourceType.Manual;

    [Column("PosReferansNo")]
    public string? PosReferenceNo { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Durum")]
    public PaymentStatus Status { get; set; } = PaymentStatus.PendingApproval;

    [Column("GirisTarihi")]
    public DateTime EntryDate { get; set; } = DateTime.Now;

    [Column("OnayTarihi")]
    public DateTime? ApprovalDate { get; set; }

    [Column("RedNedeni")]
    public string? RejectionReason { get; set; }

    public Charge Charge { get; set; } = null!;
    public ChargeLineItem ChargeLineItem { get; set; } = null!;
    public StoreAccount StoreAccount { get; set; } = null!;
    public Lease? Lease { get; set; }
    public ApplicationUser GirenUser { get; set; } = null!;
    public ApplicationUser? OnaylayanUser { get; set; }
    public List<PaymentMatch> BankMatches { get; set; } = [];
}
