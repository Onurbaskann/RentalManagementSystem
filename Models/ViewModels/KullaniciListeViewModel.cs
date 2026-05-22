namespace KiraTakip.Models.ViewModels;

public class KullaniciListeViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
