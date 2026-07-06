using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class RezervasyonTarife : BaseEntity
{
    [Column("BirimId")]
    public int? UnitId { get; set; }

    [Column("BirimTuruId")]
    public int? UnitTypeId { get; set; }
    public int? Yil { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvRate { get; set; } = 20;
    public string? Aciklama { get; set; }

    public Unit? Unit { get; set; }
    public UnitType? UnitType { get; set; }
}
