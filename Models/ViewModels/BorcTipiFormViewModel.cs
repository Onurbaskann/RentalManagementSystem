using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class BorcTipiFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    public BorcTipiDavranisi Davranis { get; set; } = BorcTipiDavranisi.AylikSabit;

    public int Sira { get; set; } = 1;

    public bool Aktif { get; set; } = true;

    public bool Sistem { get; set; }
}
