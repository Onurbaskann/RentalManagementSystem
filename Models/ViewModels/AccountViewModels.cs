using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
