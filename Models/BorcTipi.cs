using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models;

public class BorcTipi
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Ad { get; set; } = "";

    [Required, MaxLength(20)]
    [RegularExpression(@"^[A-Z0-9_]{2,20}$", ErrorMessage = "Kod yalnızca büyük harf, rakam ve alt çizgi içerebilir (2-20 karakter).")]
    public string Kod { get; set; } = "";

    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public BorcTipiDavranisi Davranis { get; set; } = BorcTipiDavranisi.AylikSabit;

    /// <summary>
    /// Seed edilen sistem kodu — Kod/Aktif değişikliğine kapalı.
    /// </summary>
    public bool Sistem { get; set; } = false;
}
