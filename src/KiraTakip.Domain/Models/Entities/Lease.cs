using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("Sozlesmeler")]
public class Lease : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("SozlesmeGrubuId")]
    public int? LeaseGroupId { get; set; }

    [Column("AnlikBirimTuruAdi")]
    public string UnitTypeNameSnapshot { get; set; } = string.Empty;

    [Column("AnlikKullanimTuru")]
    public UnitTypeUsage UnitTypeUsageSnapshot { get; set; }

    [Column("AnlikKiralamaBicimi")]
    public UnitRentalMode RentalModeSnapshot { get; set; }

    [Column("AnlikBirimYuzolcumu")]
    public decimal UnitAreaSnapshot { get; set; }

    [Column("KiraciId")]
    public int TenantId { get; set; }

    [Column("SozlesmeNo")]
    public string LeaseNo { get; set; } = string.Empty;

    [Column("Durum")]
    public LeaseStatus Status { get; set; } = LeaseStatus.Active;

    [Column("KdvUygulanacakMi")]
    public bool IsKdvApplied { get; set; }

    [Column("BedelsizMi")]
    public bool IsRentFree { get; set; }

    [Column("VadeKuraliTipi")]
    public DueDateRuleType DueDateRuleType { get; set; } = DueDateRuleType.FixedDayOfMonth;

    [Column("VadeGunu")]
    public int DueDay { get; set; } = 1;

    [Column("BaslangicTarihi")]
    public DateTime StartDate { get; set; }

    [Column("BitisTarihi")]
    public DateTime EndDate { get; set; }

    [Column("FesihTarihi")]
    public DateTime? TerminationDate { get; set; }

    [Column("FesihNedeni")]
    public string? TerminationReason { get; set; }

    [Column("Aciklama")]
    public string? Description { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public Unit Unit { get; set; } = null!;
    public LeaseGroup? LeaseGroup { get; set; }
    public LeaseArea? LeaseArea { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public List<LeaseActivityLog> ActivityLog { get; set; } = [];
    public List<LeaseRateOverride> LeaseRateOverrides { get; set; } = [];
    public List<LeaseReviewHistory> ReviewHistory { get; set; } = [];
}
