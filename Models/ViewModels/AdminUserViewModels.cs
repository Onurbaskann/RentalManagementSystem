using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class KullaniciListeViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class KullaniciEkleViewModel
{
    [Required(ErrorMessage = "Ad Soyad gereklidir.")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçiniz.")]
    public string Rol { get; set; } = string.Empty;

    public List<int> SelectedTasinmazIds { get; set; } = new();
    public List<TasinmazYetkiCheckboxViewModel> Tasinmazlar { get; set; } = new();
}

public class KullaniciDuzenleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçiniz.")]
    public string Rol { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }

    public List<int> SelectedTasinmazIds { get; set; } = new();
    public List<TasinmazYetkiCheckboxViewModel> Tasinmazlar { get; set; } = new();

    public List<string> SelectedPermissions { get; set; } = new();
    public List<PermissionGrupViewModel> YoneticiPermissions { get; set; } = new();
    public List<PermissionCheckboxViewModel> GoruntuleyiciPermissions { get; set; } = new();
}

public class PermissionCheckboxViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Etiket { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public class PermissionGrupViewModel
{
    public string GrupAdi { get; set; } = string.Empty;
    public List<PermissionCheckboxViewModel> Permissions { get; set; } = new();
}

public class TasinmazYetkiCheckboxViewModel
{
    public int TasinmazId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Konum { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
