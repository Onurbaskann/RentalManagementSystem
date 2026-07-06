namespace KiraTakip.Models.ViewModels;

public class KullaniciDuzenleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public List<RolSecenekViewModel> Roller { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool TumTasinmazlaraErisim { get; set; }
    public List<int> SelectedTasinmazIds { get; set; } = [];
    public List<TasinmazYetkiCheckboxViewModel> Properties { get; set; } = [];
    public List<int> SelectedBirimIds { get; set; } = [];
    public List<BirimYetkiCheckboxViewModel> Units { get; set; } = [];
}
