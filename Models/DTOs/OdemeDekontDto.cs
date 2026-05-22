namespace KiraTakip.Models.Dtos;

public class OdemeDekontDto
{
    public int Id { get; set; }
    public string OrijinalDosyaAdi { get; set; } = string.Empty;
    public string DiskDosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public DateTime YuklemeTarihi { get; set; }
}
