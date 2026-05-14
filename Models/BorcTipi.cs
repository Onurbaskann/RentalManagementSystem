using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models;

public class BorcTipi
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Ad { get; set; } = "";

    [Required, MaxLength(20)]
    [RegularExpression(@"^[a-zA-Z0-9_\u00C7\u011E\u0130\u00D6\u015E\u00DC\u00E7\u011F\u0131\u00F6\u015F\u00FC\s]{2,50}$", ErrorMessage = "Kod yalnızca harf, rakam, alt çizgi ve boşluk içerebilir.")]
    public string Kod { get; set; } = "";

    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public BorcTipiDavranisi Davranis { get; set; } = BorcTipiDavranisi.AylikSabit;

    /// <summary>
    /// Seed edilen sistem kodu — Kod/Aktif değişikliğine kapalı.
    /// </summary>
    public bool Sistem { get; set; } = false;
}
