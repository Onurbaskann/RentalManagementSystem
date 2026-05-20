namespace KiraTakip.Models.Entities;

public class BorcTipi : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public BorcTipiDavranisi Davranis { get; set; } = BorcTipiDavranisi.AylikSabit;

    /// <summary>
    /// Seed edilen sistem kodu — Kod/Aktif değişikliğine kapalı.
    /// </summary>
    public bool Sistem { get; set; } = false;
}
