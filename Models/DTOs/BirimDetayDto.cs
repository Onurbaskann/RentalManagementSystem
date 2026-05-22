namespace KiraTakip.Models.Dtos;

public class BirimDetayDto
{
    public int Id { get; set; }
    public string? BirimNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int? KatNo { get; set; }
    public decimal Yuzolcumu { get; set; }
    public string BirimTuruAd { get; set; } = string.Empty;
    public bool RezervasyonYapilabilirMi { get; set; }
    public bool KiralanabilirMi { get; set; }
    public KiraDurumu Durum { get; set; }
    public int? AktifSozlesmeId { get; set; }
    public int? AktifSozlesmeKiraciId { get; set; }
    public string? AktifSozlesmeKiraciGosterimAdi { get; set; }
    public DateTime? AktifSozlesmeBitisTarihi { get; set; }
    public decimal AylikBedel { get; set; }
    public int? RezKuralId { get; set; }
    public decimal? RezKuralPeriyotUcreti { get; set; }
    public int? RezKuralUcretlendirmePeriyoduDakika { get; set; }
    public int? RezKuralUcretsizSureDakika { get; set; }
    public decimal? RezKuralKdvOrani { get; set; }
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
}
