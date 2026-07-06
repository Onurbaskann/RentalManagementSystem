using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("KiraSozlesmeleri")]
public class Lease : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("Durum")]
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    [Column("KdvUygulanacakMi")]
    public bool IsKdvApplied { get; set; }

    [Column("VadeKuraliTipi")]
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;

    [Column("VadeGunu")]
    public int DueDay { get; set; } = 1;

    [Column("StartDate")]
    public DateTime StartDate { get; set; }

    [Column("EndDate")]
    public DateTime EndDate { get; set; }

    [Column("FesihTarihi")]
    public DateTime? TerminationDate { get; set; }

    [Column("FesihNedeni")]
    public string? TerminationReason { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    public Unit Unit { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public List<SozlesmeIslemGecmisi> ActivityLog { get; set; } = [];
    public List<SozlesmeTarife> LeaseRateOverrides { get; set; } = [];
}
