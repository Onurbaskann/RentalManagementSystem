using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class TahakkukKalemi : BaseEntity
{
    public int TahakkukId { get; set; }
    public int BorcTipiId { get; set; }
    public string Aciklama { get; set; } = string.Empty;

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal Carpan { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public LineItemSourceType KaynakTipi { get; set; }

    public Tahakkuk Tahakkuk { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
