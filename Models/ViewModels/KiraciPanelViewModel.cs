namespace KiraTakip.Models.ViewModels;

public class KiraciPanelViewModel
{
    public string KiraciAd { get; set; } = string.Empty;
    public string KullaniciAd { get; set; } = string.Empty;
    public string KullaniciRol { get; set; } = string.Empty;
    public string TarihEtiket { get; set; } = string.Empty;

    // KPI sayıları
    public int AktifSozlesmeAdedi { get; set; }
    public decimal ToplamAcikBorc { get; set; }
    public int YaklasanOdemeAdet { get; set; }
    public decimal YaklasanOdemeTutar { get; set; }
    public int GecikmisAdet { get; set; }
    public decimal GecikmisTutar { get; set; }

    // ApexCharts datasets
    public List<KiraciPanelAylikNakit> AylikNakit { get; set; } = [];
    public List<KiraciPanelBorcDilim> BorcTipiDagilimi { get; set; } = [];
    public List<decimal> BorcBakiyesiSparkline { get; set; } = [];

    // Listeler
    public List<KiraciPanelYaklasanItem> YaklasanTahakkuklar { get; set; } = [];
    public List<KiraciPanelSonOdemeItem> SonOdemeler { get; set; } = [];
}

public class KiraciPanelAylikNakit
{
    public string AyEtiket { get; set; } = string.Empty; // "Oca", "Şub" vb.
    public decimal Beklenen { get; set; }
    public decimal Odenen { get; set; }
}

public class KiraciPanelBorcDilim
{
    public string Ad { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class KiraciPanelYaklasanItem
{
    public int ChargeId { get; set; }
    public string Donem { get; set; } = string.Empty;       // "Mayıs 2026"
    public string BirimAd { get; set; } = string.Empty;     // "A101 / Bina A"
    public DateTime DueDate { get; set; }
    public int GunFarki { get; set; }                        // <0 = gecikmiş
    public decimal Kalan { get; set; }
    public string BorderRenk { get; set; } = string.Empty;  // "red" | "amber" | "emerald"
}

public class KiraciPanelSonOdemeItem
{
    public int OdemeId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string KanalAd { get; set; } = string.Empty;
    public string DurumAd { get; set; } = string.Empty;
    public string DurumDotRenk { get; set; } = string.Empty; // "emerald" | "amber" | "red"
}
