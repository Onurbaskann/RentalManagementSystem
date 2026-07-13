namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatViewModel
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public bool IsLeasable { get; set; }
    public bool IsReservable { get; set; }
    public string? UnitTypeName { get; set; }

    // Senaryo A — kira: KiraciKategori × ChargeType matrisi
    public List<UnitRateCategoryRow> Rows { get; set; } = [];
    public List<UnitRateColumn> Columns { get; set; } = [];
    public ParentTarifeKartViewModel? ParentTarife { get; set; }

    // Senaryo B — reservation ücreti kuralı
    public ReservationRateOverride? OzelRezervasyonKural { get; set; }
    public ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride { get; set; }
}
