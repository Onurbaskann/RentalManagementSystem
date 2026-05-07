using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class RezervasyonCreateViewModel
{
    [Required(ErrorMessage = "Toplantı salonu seçimi zorunludur.")]
    public int BirimId { get; set; }

    [Required(ErrorMessage = "Kiracı seçimi zorunludur.")]
    public int KiraciId { get; set; }

    public int? KiraSozlesmesiId { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddHours(2);

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    // Dropdown listeleri
    public List<Birim> RezervasyonBirimleri { get; set; } = [];
    public List<Kiraci> Kiraciler { get; set; } = [];
    public List<KiraSozlesmesi> Sozlesmeler { get; set; } = [];

    // Hesaplama önizlemesi
    public RezervasyonHesapSonucu? Hesap { get; set; }
}
