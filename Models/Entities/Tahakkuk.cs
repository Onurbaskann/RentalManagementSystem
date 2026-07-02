namespace KiraTakip.Models.Entities;

public class Tahakkuk : BaseEntity
{
    public int KiraciId { get; set; }
    public int BirimId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public int? RezervasyonId { get; set; }
    public DateTime DonemBaslangic { get; set; }
    public DateTime DonemBitis { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal BeklenenTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public ChargeStatus Durum { get; set; } = ChargeStatus.Pending;
    public ChargeSourceType KaynakTipi { get; set; } = ChargeSourceType.Lease;
    public string? IptalNotu { get; set; }
    public DateTime? SonHatirlatmaTarihi { get; set; }

    public Kiraci Kiraci { get; set; } = null!;
    public Birim Birim { get; set; } = null!;
    public Sozlesme? KiraSozlesmesi { get; set; }
    public Rezervasyon? Rezervasyon { get; set; }
    public List<TahakkukOdeme> Odemeler { get; set; } = [];
    public ICollection<TahakkukKalemi> Kalemler { get; set; } = [];
}
