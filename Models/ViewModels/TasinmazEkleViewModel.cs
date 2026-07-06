namespace KiraTakip.Models.ViewModels;

public class TasinmazEkleViewModel
{
    public string Ad { get; set; } = string.Empty;
    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
    public int? TasinmazTipiId { get; set; }
    public RentalMode RentalMode { get; set; } = RentalMode.WholeProperty;
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
