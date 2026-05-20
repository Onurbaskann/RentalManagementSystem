namespace KiraTakip.Models;

public class RezervasyonUcret
{
    public int Id { get; set; }

    // Birime özel kural: BirimId dolu, BirimTuruId + Yil null
    public int? BirimId { get; set; }
    public Birim? Birim { get; set; }

    // Yıllık genel tarife: BirimTuruId + Yil dolu, BirimId null
    public int? BirimTuruId { get; set; }
    public BirimTuru? BirimTuru { get; set; }
    public int? Yil { get; set; }

    public bool Aktif { get; set; } = true;

    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; } = 20;

    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
