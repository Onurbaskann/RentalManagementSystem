using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class ManuelBorcCreateViewModel
{
    [Required(ErrorMessage = "Sözleşme seçimi zorunludur.")]
    public int SozlesmeId { get; set; }

    [Required(ErrorMessage = "Borç tipi seçimi zorunludur.")]
    public int BorcTipiId { get; set; }

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [MaxLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    public string Aciklama { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tutar zorunludur.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    public decimal Tutar { get; set; }

    public bool KdvUygulanacakMi { get; set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0-100 arasında olmalıdır.")]
    public decimal KdvOrani { get; set; } = 20;

    [Required(ErrorMessage = "Vade tarihi zorunludur.")]
    public DateTime VadeTarihi { get; set; } = DateTime.Today;

    [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    public string? Not { get; set; }

    // Dropdown verileri
    public List<KiraSozlesmesi> AktifSozlesmeler { get; set; } = new();
    public List<BorcTipi> BorcTipleri { get; set; } = new();
}

public class ManuelBorcIptalViewModel
{
    public int TahakkukId { get; set; }

    [Required(ErrorMessage = "İptal nedeni zorunludur.")]
    [MaxLength(500, ErrorMessage = "İptal nedeni en fazla 500 karakter olabilir.")]
    public string Neden { get; set; } = string.Empty;
}
