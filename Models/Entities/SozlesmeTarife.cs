using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class SozlesmeTarife : BaseEntity
{
    public int KiraSozlesmesiId { get; set; }
    public int BorcTipiId { get; set; }

    [Column("HesaplamaYontemi")]
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }

    public Sozlesme KiraSozlesmesi { get; set; } = null!;
    public BorcTipi BorcTipi { get; set; } = null!;
}
