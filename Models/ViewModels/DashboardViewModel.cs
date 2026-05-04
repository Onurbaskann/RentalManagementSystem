namespace KiraTakip.Models.ViewModels;

public class DashboardViewModel
{
    public int ToplamTasinmaz { get; set; }
    public Dictionary<TasinmazTipi, int> TipiDagilim { get; set; } = new();

    public int ToplamBirim { get; set; }
    public int KiraliBirim { get; set; }
    public int BosBirim { get; set; }
    public int SuresiDolmakUzereBirim { get; set; }

    public int AktifSozlesme { get; set; }
    public int BuAyYenilenecek { get; set; }

    public decimal AylikToplamGelir { get; set; }
    public decimal YillikProj { get; set; }

    public List<SuresiDolmakUzereSozlesme> SuresiDolmakUzere { get; set; } = new();
    public List<BosBirimOzet> BosBirimler { get; set; } = new();

    // Ödeme KPI'ları
    public bool HasOdemeAccess { get; set; }
    public decimal BuAyBeklenenTahsilat { get; set; }
    public decimal BuAyTahsilEdilen { get; set; }
    public int GecikmisTahakkukAdet { get; set; }
    public decimal GecikmisTutarToplam { get; set; }
    public int OnayBekleyenOdemeAdet { get; set; }
    public int EslesmemisHareketAdet { get; set; }
}

public class SuresiDolmakUzereSozlesme
{
    public int SozlesmeId { get; set; }
    public string KiraciAdi { get; set; } = string.Empty;
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public int KalanGun { get; set; }
    public DateTime BitisTarihi { get; set; }
}

public class BosBirimOzet
{
    public int BirimId { get; set; }
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
}
