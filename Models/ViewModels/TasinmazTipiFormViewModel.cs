using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class TasinmazTipiFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    public int Sira { get; set; } = 1;
    public bool Aktif { get; set; } = true;
    public bool TekParcaDestekli { get; set; }
    public bool BirimBazliDestekli { get; set; }
}
