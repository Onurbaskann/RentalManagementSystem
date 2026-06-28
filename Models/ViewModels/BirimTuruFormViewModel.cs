using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class BirimTuruFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; } = 1;
    public bool KiralanabilirMi { get; set; } = true;
    public bool RezervasyonYapilabilirMi { get; set; }
    public int? BorcTipiId { get; set; }
    public bool Aktif { get; set; } = true;

    public List<BorcTipiLookupDto> BorcTipiAdaylari { get; set; } = [];
}
