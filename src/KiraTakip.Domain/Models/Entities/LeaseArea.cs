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

    [Column("FizikselAlan")]
    public decimal AllocatedArea { get; set; }

    [Column("HesaplamaModu")]
    public LeaseAreaMode AllocationMode { get; set; } = LeaseAreaMode.Manual;

    public Lease Lease { get; set; } = null!;
}
