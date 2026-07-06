using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Reservations")]
public class Reservation : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("KiraciId")]
    public int TenantId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalDurationMinutes { get; set; }

    public int FreeDurationMinutes { get; set; }

    public int PaidDurationMinutes { get; set; }

    public decimal UnitRate { get; set; }

    public decimal RateAmount { get; set; }

    public decimal? KdvRate { get; set; }

    [Column("KdvTutari")]
    public decimal? KdvAmount { get; set; }

    [Column("ToplamTutar")]
    public decimal TotalAmount { get; set; }

    [Column("Durum")]
    public ReservationStatus Status { get; set; } = ReservationStatus.Planned;

    [Column("Aciklama")]
    public string? Description { get; set; }

    public Unit Unit { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
