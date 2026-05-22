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

    [Required(ErrorMessage = "Kod zorunludur.")]
    [MaxLength(50)]
    public string Kod { get; set; } = string.Empty;

    public int Sira { get; set; } = 1;
    public bool Aktif { get; set; } = true;

    // Sadece TasinmazTipi ekranında bind edilir
    public bool TekParcaDestekli { get; set; }
    public bool BirimBazliDestekli { get; set; }
}
