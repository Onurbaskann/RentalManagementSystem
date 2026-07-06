using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class KullaniciEkleViewModel
{
    public string AdSoyad { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public List<int> SelectedTasinmazIds { get; set; } = [];
    public List<TasinmazYetkiCheckboxViewModel> Properties { get; set; } = [];
}
