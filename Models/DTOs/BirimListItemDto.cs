namespace KiraTakip.Models.Dtos;

public class BirimListItemDto
{
    public int Id { get; set; }
    public string? BirimNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int? KatNo { get; set; }
    public decimal Yuzolcumu { get; set; }
    public string BirimTuruAd { get; set; } = string.Empty;
    public KiraDurumu Durum { get; set; }
    public decimal AylikBedel { get; set; }
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
}
