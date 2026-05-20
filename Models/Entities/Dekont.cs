namespace KiraTakip.Models.Entities;

public class Dekont : BaseEntity
{
    public int KiraOdemeId { get; set; }
    public string YukleyenUserId { get; set; } = string.Empty;
    public string OrijinalDosyaAdi { get; set; } = string.Empty;
    public string DiskDosyaAdi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;
    public string DosyaTipi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }
    public DateTime YuklemeTarihi { get; set; } = DateTime.Now;

    public KiraOdeme KiraOdeme { get; set; } = null!;
    public ApplicationUser YukleyenUser { get; set; } = null!;
}
