namespace KiraTakip.Models;

public class KiraTahakkuk
{
    public int Id { get; set; }

    public int KiraSozlesmesiId { get; set; }
    public KiraSozlesmesi KiraSozlesmesi { get; set; } = null!;

    public DateTime DonemBaslangic { get; set; }
    public DateTime DonemBitis { get; set; }
    public DateTime VadeTarihi { get; set; }

    public decimal BeklenenTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }

    public TahakkukDurumu Durum { get; set; } = TahakkukDurumu.Bekleniyor;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public List<KiraOdeme> Odemeler { get; set; } = new();
    public ICollection<TahakkukKalemi> Kalemler { get; set; } = new List<TahakkukKalemi>();
}
