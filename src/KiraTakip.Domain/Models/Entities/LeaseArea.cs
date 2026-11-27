using KiraTakip.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeAlanlari")]
public class LeaseArea : BaseEntity
{
    [Column("SozlesmeId")]
    public int LeaseId { get; set; }

    [Column("KiralananAlan")]
    public decimal RentedArea { get; set; }

    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("AnlikKod")]
    public string SnapshotCode { get; set; } = string.Empty;

    [Column("AnlikAd")]
    public string SnapshotName { get; set; } = string.Empty;

    [Column("AnlikTurAdi")]
    public string SnapshotTypeName { get; set; } = string.Empty;

    [Column("AnlikKullanim")]
    public SpaceUsage SnapshotUsage { get; set; }

    [Column("AnlikYuzolcumu")]
    public decimal SnapshotArea { get; set; }

    [Column("AnlikYol")]
    public string SnapshotPath { get; set; } = string.Empty;

    public Lease Lease { get; set; } = null!;
    public Space Space { get; set; } = null!;
}
