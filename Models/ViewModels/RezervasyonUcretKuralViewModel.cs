using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class RezervasyonUcretKuralViewModel
{
    public int Id { get; set; }

    public int? BirimId { get; set; }

    [Required(ErrorMessage = "Ücretsiz süre zorunludur.")]
    [Range(0, 1440, ErrorMessage = "0–1440 dakika arası olmalıdır.")]
    public int UcretsizSureDakika { get; set; }

    [Required(ErrorMessage = "Periyot süresi zorunludur.")]
    [Range(1, 1440, ErrorMessage = "1–1440 dakika arası olmalıdır.")]
    public int UcretlendirmePeriyoduDakika { get; set; } = 60;

    [Required(ErrorMessage = "Periyot ücreti zorunludur.")]
    [Range(0, double.MaxValue, ErrorMessage = "Tutar sıfır veya daha büyük olmalıdır.")]
    public decimal PeriyotUcreti { get; set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0–100 arasında olmalıdır.")]
    public decimal KdvOrani { get; set; } = 20;

    public bool Aktif { get; set; } = true;

    [MaxLength(300)]
    public string? Aciklama { get; set; }

    // Dropdown
    public List<Birim> RezervasyonBirimleri { get; set; } = [];
}
