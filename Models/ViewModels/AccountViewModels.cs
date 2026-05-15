using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class LoginViewModel
{
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
