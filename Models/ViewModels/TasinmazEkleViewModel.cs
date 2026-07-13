namespace KiraTakip.Models.ViewModels;

public class TasinmazEkleViewModel
{
    public string Ad { get; set; } = string.Empty;
    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride { get; set; }
    public int? TasinmazTipiId { get; set; }
    public UnitStructure UnitStructure { get; set; } = UnitStructure.SingleUnit;
    public int? KompleUnitTypeId { get; set; }
    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;
    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }
    public int? KatSayisi { get; set; }
    public string? Aciklama { get; set; }
    public List<BirimInputViewModel> Units { get; set; } = [];
    public List<RezervasyonAlaniInputViewModel> RezervasyonAlanlari { get; set; } = [];
    public TasinmazFiyatMatrisiViewModel? FiyatMatrisi { get; set; }
}
