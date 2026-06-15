using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class RezervasyonCreateViewModel
{
    [Required(ErrorMessage = "{0} seçilmelidir.")]
    [Display(Name = "Taşınmaz Birimi")]
    public int? BirimId { get; set; }

    [Required(ErrorMessage = "{0} seçilmelidir.")]
    [Display(Name = "Kiracı")]
    public int? KiraciId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime BitisTarihi { get; set; } = DateTime.Today.AddHours(2);

    [MaxLength(500)]
    public string? Aciklama { get; set; }
    public List<BirimListItemDto> RezervasyonBirimleri { get; set; } = [];
    public List<KiraciListItemDto> Kiraciler { get; set; } = [];
    public List<SozlesmeListItemDto> Sozlesmeler { get; set; } = [];
    public RezervasyonHesapSonucu? Hesap { get; set; }
}
