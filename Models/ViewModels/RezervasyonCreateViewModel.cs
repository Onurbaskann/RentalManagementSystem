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
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddHours(2);

    [MaxLength(500)]
    public string? Aciklama { get; set; }
    public List<BirimListItemDto> RezervasyonBirimleri { get; set; } = [];
    public List<KiraciListItemDto> Tenants { get; set; } = [];
    public RezervasyonHesapSonucu? Hesap { get; set; }
}
