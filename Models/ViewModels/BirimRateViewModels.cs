namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatViewModel
{
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = "";
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = "";

    public bool KiralanabilirMi { get; set; }
    public bool RezervasyonYapilabilirMi { get; set; }
    public string? BirimTuruAd { get; set; }

    // Senaryo A — kira: KiraciKategori × BorcTipi matrisi
    public List<BirimRateKategoriSatiri> Satirlar { get; set; } = [];
    public List<BirimRateKolonu> Kolonlar { get; set; } = [];
    public ParentTarifeKartViewModel? ParentTarife { get; set; }

    // Senaryo B — rezervasyon ücreti kuralı
    public RezervasyonUcret? OzelRezervasyonKural { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
}

public class BirimRateKategoriSatiri
{
    public int KiraciKategoriId { get; set; }
    public string KiraciKategoriAd { get; set; } = "";
    public List<BirimRateHucre> Hucreler { get; set; } = [];
}

public class BirimRateKolonu
{
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = "";
    public string BorcTipiKod { get; set; } = "";
    public BorcTipiDavranisi BorcTipiDavranisi { get; set; }
}

public class BirimRateHucre
{
    public int RateId { get; set; }
    public int KiraciKategoriId { get; set; }
    public int BorcTipiId { get; set; }
    public bool OzelFiyatAktif { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}
