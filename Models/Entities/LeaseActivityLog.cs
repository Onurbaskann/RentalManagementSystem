using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeIslemGecmisleri")]
public class LeaseActivityLog : BaseEntity
{
    [Column("SozlesmeId")]
    public int LeaseId { get; set; }

    [Column("IslemTipi")]
    public LeaseActivityType ActivityType { get; set; }

    [Column("IslemTarihi")]
    public DateTime TransactionDate { get; set; }

    [Column("Aciklama")]
    public string Description { get; set; } = string.Empty;

    [Column("EskiBitisTarihi")]
    public DateTime? OldEndDate { get; set; }

    [Column("YeniBitisTarihi")]
    public DateTime? NewEndDate { get; set; }

    [Column("EskiKiraBedeli")]
    public decimal? OldRentAmount { get; set; }

    [Column("YeniKiraBedeli")]
    public decimal? NewRentAmount { get; set; }

    [Column("TufeOrani")]
    public decimal? InflationRate { get; set; }

    [Column("KdvUygulandiMi")]
    public bool? IsKdvApplied { get; set; }

    [Column("KdvOrani")]
    public decimal? KdvRate { get; set; }

    [Column("KdvTutari")]
    public decimal? KdvAmount { get; set; }

    [Column("KdvDahilTutar")]
    public decimal? KdvIncludedAmount { get; set; }

    public Lease? Lease { get; set; }
}
