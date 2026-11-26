using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("AlanAdresleri")]
public class SpaceAddress : BaseEntity
{
    [Column("AlanId")]
    public int SpaceId { get; set; }

    [Column("Il")]
    public string City { get; set; } = string.Empty;

    [Column("Ilce")]
    public string District { get; set; } = string.Empty;

    [Column("Mahalle")]
    public string Neighborhood { get; set; } = string.Empty;

    [Column("AcikAdres")]
    public string Address { get; set; } = string.Empty;

    public Space Space { get; set; } = null!;
}
