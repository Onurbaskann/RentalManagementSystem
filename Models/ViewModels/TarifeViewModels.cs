namespace KiraTakip.Models.ViewModels;

public class TarifeYilOzetiViewModel
{
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public int KalemSayisi { get; set; }
}

// GET ViewModel — matris yapısı
public class TarifeMatrisViewModel
{
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisBorcTipiKolon> Kolonlar { get; set; } = [];
    public List<TarifeMatrisSatir> Satirlar { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonSatirlari { get; set; } = [];
}

public class TarifeMatrisRezervasyonSatir
{
    public int RezervasyonTarifeId { get; set; }
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
    public int Yil { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeMatrisHucre> Hucreler { get; set; } = [];
    public List<TarifeMatrisRezervasyonSatir> RezervasyonHucreler { get; set; } = [];
}

public class TarifeDetayViewModel
{
    public int Yil { get; set; }
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
    public int? KopyalaYil { get; set; }
}
