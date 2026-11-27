using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("SozlesmeGruplari")]
public class LeaseGroup : BaseEntity
{
    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("OncekiGrupId")]
    public int? PreviousGroupId { get; set; }

    public Space Space { get; set; } = null!;
    public LeaseGroup? PreviousGroup { get; set; }
    public List<Lease> Leases { get; set; } = [];
}
