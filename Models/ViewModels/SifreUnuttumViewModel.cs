using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class SifreUnuttumViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;
}
