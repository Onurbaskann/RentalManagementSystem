namespace KiraTakip.Models.ViewModels;

public class KullaniciDuzenleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
    public List<int> SelectedTasinmazIds { get; set; } = [];
    public List<TasinmazYetkiCheckboxViewModel> Tasinmazlar { get; set; } = [];
    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGrupViewModel> YoneticiPermissions { get; set; } = [];
    public List<PermissionCheckboxViewModel> GoruntuleyiciPermissions { get; set; } = [];
}
