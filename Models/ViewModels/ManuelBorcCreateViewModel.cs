using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ManuelBorcCreateViewModel
{
    public int KiraciId { get; set; }
    public int? SozlesmeId { get; set; }
    public int BirimId { get; set; }
    public int ChargeTypeId { get; set; }

    [MaxLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    public string Aciklama { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount sıfırdan büyük olmalıdır.")]
    public decimal Amount { get; set; }
    public bool KdvUygulanacakMi { get; set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0-100 arasında olmalıdır.")]
    public decimal KdvRate { get; set; } = 20;
    public DateTime DueDate { get; set; } = DateTime.Today;

    [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    public string? Not { get; set; }
    public List<SozlesmeDropdownDto> AktifSozlesmeler { get; set; } = [];
    public List<BorcTipiLookupDto> ChargeTypes { get; set; } = [];
    public List<BirimLookupDto> Units { get; set; } = [];
}
