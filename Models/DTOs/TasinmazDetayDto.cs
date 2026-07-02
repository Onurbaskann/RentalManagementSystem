namespace KiraTakip.Models.Dtos;

public class TasinmazDetayDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Mahalle { get; set; } = string.Empty;
    public string AcikAdres { get; set; } = string.Empty;
    public string TasinmazTipiAd { get; set; } = string.Empty;
    public decimal KapaliYuzolcumu { get; set; }
    public decimal AcikYuzolcumu { get; set; }
    public RentalMode RentalMode { get; set; }
    public string? Aciklama { get; set; }
    public List<BirimDetayDto> Birimler { get; set; } = [];
    public List<TasinmazRezervasyonDto> Rezervasyonlar { get; set; } = [];
    public List<BirimRezervasyonKuralDto> BirimRezervasyonKurallari { get; set; } = [];
    public List<BirimOzelFiyatOzetDto> BirimOzelFiyatlari { get; set; } = [];
    public List<TasinmazSozlesmeGecmisiDto> SozlesmeGecmisi { get; set; } = [];
}
