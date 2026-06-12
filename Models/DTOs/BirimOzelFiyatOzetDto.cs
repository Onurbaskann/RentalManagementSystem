namespace KiraTakip.Models.Dtos;

public class BirimOzelFiyatOzetDto
{
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public string? BirimNo { get; set; }
    public List<BirimOzelFiyatRateDto> Rateler { get; set; } = [];
}
