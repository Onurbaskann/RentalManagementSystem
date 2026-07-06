using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class BelgeTuruFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(200)]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    [Required]
    public BelgeOwnerTipi HedefEntite { get; set; } = BelgeOwnerTipi.Tenant;

    public bool Zorunlu { get; set; }

    [Required(ErrorMessage = "İzin verilen uzantılar zorunludur.")]
    [MaxLength(200)]
    public string IzinVerilenUzantilar { get; set; } = "pdf,jpg,png";

    [Range(1, 100, ErrorMessage = "Maksimum boyut 1-100 MB arasında olmalıdır.")]
    public int MaxBoyutMb { get; set; } = 5;

    [Range(1, 9999)]
    public int Sira { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public bool Sistem { get; set; }
}
