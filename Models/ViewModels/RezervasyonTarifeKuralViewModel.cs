using KiraTakip.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class RezervasyonTarifeKuralViewModel
{
    public int Id { get; set; }

    public int? BirimId { get; set; }

    [Range(0, 1440, ErrorMessage = "0–1440 dakika arası olmalıdır.")]
    public int UcretsizSureDakika { get; set; }

    [Range(1, 1440, ErrorMessage = "1–1440 dakika arası olmalıdır.")]
    public int UcretlendirmePeriyoduDakika { get; set; } = 60;

    [Range(0, double.MaxValue, ErrorMessage = "Tutar sıfır veya daha büyük olmalıdır.")]
    public decimal PeriyotUcreti { get; set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0–100 arasında olmalıdır.")]
    public decimal KdvOrani { get; set; } = 20;

    public bool Aktif { get; set; } = true;

    [MaxLength(300)]
    public string? Aciklama { get; set; }

    public List<Birim> RezervasyonBirimleri { get; set; } = [];
}
