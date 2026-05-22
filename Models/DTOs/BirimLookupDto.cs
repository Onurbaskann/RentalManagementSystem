namespace KiraTakip.Models.Dtos;

public class BirimLookupDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string TasinmazAd { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string Il { get; set; } = string.Empty;
    public decimal Yuzolcumu { get; set; }
    public BirimTipi BirimTipi { get; set; }
    public string? BirimNo { get; set; }
    public int? KatNo { get; set; }
}
