using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

public class TahakkukOdeme : BaseEntity
{
    public int TahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public string GirenUserId { get; set; } = string.Empty;
    public string? OnaylayanUserId { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }

    [Column("OdemeKanali")]
    public PaymentChannel PaymentChannel { get; set; }

    [Column("OdemeKaynakTipi")]
    public PaymentSourceType PaymentSourceType { get; set; } = PaymentSourceType.Manual;
    public string? PosReferansNo { get; set; }
    public string? Aciklama { get; set; }
    public PaymentStatus Durum { get; set; } = PaymentStatus.PendingApproval;
    public DateTime GirisTarihi { get; set; } = DateTime.Now;
    public DateTime? OnayTarihi { get; set; }
    public string? RedNedeni { get; set; }

    public Tahakkuk Tahakkuk { get; set; } = null!;
    public Sozlesme? KiraSozlesmesi { get; set; }
    public ApplicationUser GirenUser { get; set; } = null!;
    public ApplicationUser? OnaylayanUser { get; set; }
    public List<OdemeBankaEslesme> BankaEslesmeleri { get; set; } = [];
}
