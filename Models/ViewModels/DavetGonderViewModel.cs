using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class DavetGonderViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    public string? AdSoyad { get; set; }

    [Required(ErrorMessage = "Rol seçimi zorunludur.")]
    public int RolId { get; set; }

    public List<RolSecenekViewModel> Roller { get; set; } = [];

    public bool TumTasinmazlaraErisim { get; set; } = false;
    public List<int> SelectedTasinmazIds { get; set; } = [];
    public List<TasinmazYetkiCheckboxViewModel> Tasinmazlar { get; set; } = [];
    public List<int> SelectedBirimIds { get; set; } = [];
    public List<BirimYetkiCheckboxViewModel> Birimler { get; set; } = [];
}

public class RolSecenekViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
}
