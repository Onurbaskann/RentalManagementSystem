using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class KategoriFormViewModel
{
    public int Id { get; set; }
    public KategoriTipi Tipi { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; } = 1;
    public bool Aktif { get; set; } = true;
}
