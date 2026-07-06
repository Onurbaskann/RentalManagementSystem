using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Charges")]
public class Charge : BaseEntity
{
    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("LeaseId")]
    public int? LeaseId { get; set; }

    public int? ReservationId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime DueDate { get; set; }

    public decimal ExpectedAmount { get; set; }

    [Column("KdvTutari")]
    public decimal KdvAmount { get; set; }

    [Column("ToplamTutar")]
    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    [Column("Durum")]
    public ChargeStatus Status { get; set; } = ChargeStatus.Pending;

    public ChargeSourceType SourceType { get; set; } = ChargeSourceType.Lease;

    public string? CancellationNote { get; set; }

    public DateTime? LastReminderDate { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public Lease? Lease { get; set; }
    public Reservation? Reservation { get; set; }
    public List<PaymentAllocation> Allocations { get; set; } = [];
    public ICollection<ChargeLineItem> LineItems { get; set; } = [];
}
