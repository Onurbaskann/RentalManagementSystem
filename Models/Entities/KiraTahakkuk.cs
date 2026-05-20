namespace KiraTakip.Models.Entities;

public class KiraTahakkuk : BaseEntity
{
    public int? KiraSozlesmesiId { get; set; }
    public DateTime DonemBaslangic { get; set; }
    public DateTime DonemBitis { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal BeklenenTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public TahakkukDurumu Durum { get; set; } = TahakkukDurumu.Bekleniyor;
    public TahakkukKaynakTipi KaynakTipi { get; set; } = TahakkukKaynakTipi.Sozlesme;
    public string? IptalNotu { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime? SonHatirlatmaTarihi { get; set; }

    public KiraSozlesmesi? KiraSozlesmesi { get; set; }
    public List<KiraOdeme> Odemeler { get; set; } = [];
    public ICollection<TahakkukKalemi> Kalemler { get; set; } = [];
}
