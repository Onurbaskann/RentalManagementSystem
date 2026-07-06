namespace KiraTakip.Models.ViewModels;

public class DashboardViewModel
{
    // Hero / kimlik
    public string KullaniciAd { get; set; } = string.Empty;
    public string KullaniciRol { get; set; } = string.Empty;
    public string TarihEtiket { get; set; } = string.Empty;

    public int ToplamTasinmaz { get; set; }
    public Dictionary<string, int> TipiDagilim { get; set; } = [];
    public int ToplamBirim { get; set; }
    public int KiraliBirim { get; set; }
    public int BosBirim { get; set; }
    public int SuresiDolmakUzereBirim { get; set; }
    public int AktifSozlesme { get; set; }
    public int BuAyYenilenecek { get; set; }
    public decimal AylikToplamGelir { get; set; }
    public decimal YillikProj { get; set; }
    public List<SuresiDolmakUzereSozlesme> SuresiDolmakUzere { get; set; } = [];
    public List<BosBirimOzet> BosBirimler { get; set; } = [];

    // Ödeme KPI'ları
    public bool HasOdemeAccess { get; set; }
    public decimal BuAyBeklenenTahsilat { get; set; }
    public decimal BuAyTahsilEdilen { get; set; }
    public int GecikmisTahakkukAdet { get; set; }
    public decimal GecikmisTutarToplam { get; set; }
    public int OnayBekleyenOdemeAdet { get; set; }
    public int EslesmemisHareketAdet { get; set; }

    // Reservation ve manuel borç metrikleri
    public decimal BuAyManuelBorcToplami { get; set; }
    public decimal BuAyRezervasyonGeliri { get; set; }
    public int TahakkukaAktarilmamisRezervasyonAdet { get; set; }

    // --- Yeni (Redesign) ---
    // Trend / grafikler
    public List<DashboardAylikNakit> AylikNakit { get; set; } = [];      // son 6 ay
    public List<double> TahsilatOraniSparkline { get; set; } = [];        // son 6 ay yüzde (0..100)

    // Tahsilat oranı (son 30 gün)
    public decimal TahsilatOrani30Gun { get; set; }                       // 0..100

    // Momentum — aylık gelir
    public decimal AylikGelirGecenAy { get; set; }
    public decimal AylikGelirDelta { get; set; }                          // % değişim (-100..+inf)

    // Bugün vade dolan
    public int BugunVadeDolanAdet { get; set; }
    public decimal BugunVadeDolanTutar { get; set; }

    // Top 5 gelir getiren taşınmaz (son 12 ay tahsilat)
    public List<DashboardGelirTasinmaz> TopGelirTasinmaz { get; set; } = [];

    // Top 5 gelir getiren kiracı (son 12 ay tahsilat)
    public List<DashboardGelirKiraci> TopGelirKiraci { get; set; } = [];

    public int AktifKiraciSayisi { get; set; }
}

public class DashboardAylikNakit
{
    public string AyEtiket { get; set; } = string.Empty; // "Oca", "Şub" vb.
    public decimal Beklenen { get; set; }
    public decimal Odenen { get; set; }
}

public class DashboardGelirTasinmaz
{
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
    public decimal ToplamTahsilat { get; set; }
    public int BirimSayisi { get; set; }
}

public class DashboardGelirKiraci
{
    public int KiraciId { get; set; }
    public string KiraciAd { get; set; } = string.Empty;
    public decimal ToplamTahsilat { get; set; }
    public int SozlesmeSayisi { get; set; }
}

public class SuresiDolmakUzereSozlesme
{
    public int SozlesmeId { get; set; }
    public string KiraciAdi { get; set; } = string.Empty;
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public int KalanGun { get; set; }
    public DateTime EndDate { get; set; }
}

public class BosBirimOzet
{
    public int BirimId { get; set; }
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
}
