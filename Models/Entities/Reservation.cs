using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Rezervasyonlar")]
public class Reservation : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("BaslangicTarihi")]
    public DateTime StartDate { get; set; }

    [Column("BitisTarihi")]
    public DateTime EndDate { get; set; }

    [Column("ToplamSureDakika")]
    public int TotalDurationMinutes { get; set; }

    [Column("UcretsizSureDakika")]
    public int FreeDurationMinutes { get; set; }

    [Column("UcretliSureDakika")]
    public int PaidDurationMinutes { get; set; }

    [Column("BirimUcreti")]
    public decimal UnitRate { get; set; }

    [Column("TarifeTutari")]
    public decimal RateAmount { get; set; }

    [Column("KdvOrani")]
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
