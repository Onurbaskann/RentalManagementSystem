namespace KiraTakip.Models.ViewModels;

public class TasinmazDuzenleViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int? TasinmazTipiId { get; set; }
    public RentalMode RentalMode { get; set; }
    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;
    public decimal AcikYuzolcumu { get; set; }
    public decimal KapaliYuzolcumu { get; set; }
    public int? KatSayisi { get; set; }
    public string? Aciklama { get; set; }
    public List<BirimDuzenleViewModel> Units { get; set; } = [];
    public List<RezervasyonAlaniDuzenleViewModel> RezervasyonAlanlari { get; set; } = [];
    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = new();
    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public ParentRezervasyonTarifeKartViewModel? ParentRezervasyonTarife { get; set; }
}
