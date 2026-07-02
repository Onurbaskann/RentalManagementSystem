using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class Birim : BaseEntity
{
    public int TasinmazId { get; set; }

    [Column("BirimTuruId")]
    public int? UnitTypeId { get; set; }

    [Column("BirimTipi")]
    public UnitKind UnitKind { get; set; }
    public int? KatNo { get; set; }
    public string? BirimNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
    public string? Aciklama { get; set; }

    public Tasinmaz Tasinmaz { get; set; } = null!;
    public UnitType? UnitType { get; set; }
    public List<Sozlesme> Sozlesmeler { get; set; } = [];
}
