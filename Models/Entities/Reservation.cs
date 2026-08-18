using System.ComponentModel.DataAnnotations;
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
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    [Column("Baslik")]
    public string? Title { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Column("Notlar")]
    public string? Notes { get; set; }

    [Column("IcNotlar")]
    public string? InternalNotes { get; set; }

    [Column("SonDegisiklikNedeni")]
    public string? LastModificationReason { get; set; }

    [Column("TalepEdenKullaniciId")]
    public string? RequestedByUserId { get; set; }

    [Column("TalepEdenAdSoyad")]
    public string? RequestedByDisplayNameSnapshot { get; set; }

    [Column("TalepEdenEposta")]
    public string? RequestedByEmailSnapshot { get; set; }

    [Column("OnaylayanKullaniciId")]
    public string? ApprovedByUserId { get; set; }

    [Column("OnayTarihi")]
    public DateTime? ApprovedAt { get; set; }

    [Column("ReddedenKullaniciId")]
    public string? RejectedByUserId { get; set; }

    [Column("RetTarihi")]
    public DateTime? RejectedAt { get; set; }

    [Column("RetNedeni")]
    public string? RejectionReason { get; set; }

    [Column("IptalEdenKullaniciId")]
    public string? CancelledByUserId { get; set; }

    [Column("IptalTarihi")]
    public DateTime? CancelledAt { get; set; }

    [Column("IptalNedeni")]
    public string? CancellationReason { get; set; }

    [Column("TamamlanmaTarihi")]
    public DateTime? CompletedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public Unit Unit { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ApplicationUser? RequestedByUser { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public ApplicationUser? RejectedByUser { get; set; }
    public ApplicationUser? CancelledByUser { get; set; }
    public List<ReservationAttendee> Attendees { get; set; } = [];
}
