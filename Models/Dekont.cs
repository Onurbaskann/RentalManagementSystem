namespace KiraTakip.Models;

public class Dekont
{
    public int Id { get; set; }

    public int KiraOdemeId { get; set; }
    public KiraOdeme KiraOdeme { get; set; } = null!;

    public string OrijinalDosyaAdi { get; set; } = string.Empty;
    public string DiskDosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }

    public string YukleyenUserId { get; set; } = string.Empty;
    public ApplicationUser YukleyenUser { get; set; } = null!;
    public DateTime YuklemeTarihi { get; set; } = DateTime.Now;
}
