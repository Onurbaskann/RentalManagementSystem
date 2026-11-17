using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BirimKontenjanGecmisleri")]
public class UnitCapacityHistory : BaseEntity
{
    [Column("BirimId")]
    public int UnitId { get; set; }

    [Column("Kontenjan")]
    public int Capacity { get; set; }

    [Column("GecerlilikBaslangici")]
    public DateTime EffectiveFrom { get; set; }

    public Unit Unit { get; set; } = null!;
}
