using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Tahakkuklar")]
public class Charge : BaseEntity
{
    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("SozlesmeId")]
    public int? LeaseId { get; set; }

    [Column("RezervasyonId")]
    public int? ReservationId { get; set; }

    [Column("DonemBaslangici")]
    public DateTime PeriodStart { get; set; }

    [Column("DonemBitisi")]
    public DateTime PeriodEnd { get; set; }

    [Column("SonOdemeTarihi")]
    public DateTime DueDate { get; set; }

    [Column("BeklenenTutar")]
    public decimal ExpectedAmount { get; set; }

    [Column("KdvTutari")]
    public decimal KdvAmount { get; set; }

    [Column("ToplamTutar")]
    public decimal TotalAmount { get; set; }

    [Column("OdenenTutar")]
    public decimal PaidAmount { get; set; }

    [Column("Durum")]
    public ChargeStatus Status { get; set; } = ChargeStatus.Pending;

    [Column("KaynakTipi")]
    public ChargeSourceType SourceType { get; set; } = ChargeSourceType.Lease;

    [Column("IptalNotu")]
    public string? CancellationNote { get; set; }

    [Column("SonHatirlatmaTarihi")]
    public DateTime? LastReminderDate { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public Lease? Lease { get; set; }
    public Reservation? Reservation { get; set; }
    public List<PaymentAllocation> Allocations { get; set; } = [];
    public ICollection<ChargeLineItem> LineItems { get; set; } = [];
}
