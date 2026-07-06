namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatViewModel
{
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
    public bool KiralanabilirMi { get; set; }
    public bool RezervasyonYapilabilirMi { get; set; }
    public string? UnitTypeAd { get; set; }

    // Senaryo A — kira: KiraciKategori × ChargeType matrisi
    public List<BirimTarifeKategoriSatiri> Satirlar { get; set; } = [];
    public List<BirimTarifeKolonu> Kolonlar { get; set; } = [];
    public ParentTarifeKartViewModel? ParentTarife { get; set; }

    // Senaryo B — rezervasyon ücreti kuralı
    public RezervasyonTarife? OzelRezervasyonKural { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
}
