namespace KiraTakip.Models.Dtos;

public class DekontListItemDto
{
    public int Id { get; set; }
    public int KiraOdemeId { get; set; }
    public string OrijinalDosyaAdi { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public DateTime YuklemeTarihi { get; set; }
    public string? YukleyenUserAdi { get; set; }
}
