namespace KiraTakip.Models.ViewModels;

// GET ViewModel — matris yapısı
public class TarifeMatrisViewModel
{
    public int TarifeId { get; set; }
    public int Yil { get; set; }
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisBorcTipiKolon> Kolonlar { get; set; } = [];
    public List<TarifeMatrisSatir> Satirlar { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonSatirlari { get; set; } = [];
}

public class TarifeMatrisRezervasyonSatir
{
    public int RezervasyonGenelTarifeId { get; set; }
    public int BirimTuruId { get; set; }
    public string BirimTuruAd { get; set; } = "";
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
}

public class TarifeMatrisBorcTipiKolon
{
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = "";
    public string BorcTipiKod { get; set; } = "";
}

public class TarifeMatrisSatir
{
    public int KiraciKategoriId { get; set; }
    public string KiraciKategoriAd { get; set; } = "";
    public List<TarifeMatrisHucre> Hucreler { get; set; } = [];
}

public class TarifeMatrisHucre
{
    public int KalemId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

// POST ViewModel — düz liste
public class TarifeMatrisPostViewModel
{
    public int TarifeId { get; set; }
    public int Yil { get; set; }
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisHucre> Hucreler { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonHucreler { get; set; } = [];
}

// Eski (geriye uyumluluk için — YilEkle view'ı kullanıyor)
public class TarifeDetayViewModel
{
    public int TarifeId { get; set; }
    public int Yil { get; set; }
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeKalemSatiri> Kalemler { get; set; } = [];
}

public class TarifeKalemSatiri
{
    public int KalemId { get; set; }
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = "";
    public string BorcTipiKod { get; set; } = "";
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

public class TarifeYilEkleViewModel
{
    public int Yil { get; set; } = DateTime.Now.Year;
    public string? Aciklama { get; set; }
    public int? KopyalaYilId { get; set; }
}
