using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeGruplari")]
public class LeaseGroup : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("OncekiGrupId")]
    public int? PreviousGroupId { get; set; }

    public Unit Unit { get; set; } = null!;
    public LeaseGroup? PreviousGroup { get; set; }
    public List<Lease> Leases { get; set; } = [];
}
