using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class TasinmazTarife : BaseEntity
{
    public int TasinmazId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Tasinmaz Tasinmaz { get; set; } = null!;
    public Kategori KiraciKategori { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
