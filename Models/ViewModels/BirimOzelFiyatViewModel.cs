namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatViewModel
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public bool IsLeasable { get; set; }
    public bool IsReservable { get; set; }
    public string? UnitTypeAd { get; set; }

    // Senaryo A — kira: KiraciKategori × ChargeType matrisi
    public List<UnitRateCategoryRow> Satirlar { get; set; } = [];
    public List<UnitRateColumn> Kolonlar { get; set; } = [];
    public ParentTarifeKartViewModel? ParentTarife { get; set; }

    // Senaryo B — reservation ücreti kuralı
    public RezervasyonTarife? OzelRezervasyonKural { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
}
