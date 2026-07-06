using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("PaymentAllocations")]
public class PaymentAllocation : BaseEntity
{
    public int ChargeId { get; set; }

    [Column("LeaseId")]
    public int? LeaseId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? ApprovedByUserId { get; set; }

    public DateTime PaymentDate { get; set; }

    public decimal Amount { get; set; }

    [Column("OdemeKanali")]
    public PaymentChannel PaymentChannel { get; set; }

    [Column("OdemeKaynakTipi")]
    public PaymentSourceType PaymentSourceType { get; set; } = PaymentSourceType.Manual;

    public string? PosReferenceNo { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Durum")]
    public PaymentStatus Status { get; set; } = PaymentStatus.PendingApproval;

    public DateTime EntryDate { get; set; } = DateTime.Now;

    public DateTime? ApprovalDate { get; set; }

    public string? RejectionReason { get; set; }

    public Charge Charge { get; set; } = null!;
    public Lease? Lease { get; set; }
    public ApplicationUser GirenUser { get; set; } = null!;
    public ApplicationUser? OnaylayanUser { get; set; }
    public List<PaymentMatch> BankMatches { get; set; } = [];
}
