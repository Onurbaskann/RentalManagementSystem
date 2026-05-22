namespace KiraTakip.Models.ViewModels;

public class PermissionGrupViewModel
{
    public string GrupAdi { get; set; } = string.Empty;
    public List<PermissionCheckboxViewModel> Permissions { get; set; } = [];
}
