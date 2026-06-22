namespace KiraTakip.Models.ViewModels;

public class KullaniciListeViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class KiraciKullaniciListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciAd { get; set; } = string.Empty;
    public string RolAd { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminKullaniciIndexViewModel
{
    public List<KullaniciListeViewModel> IcKullanicilar { get; set; } = [];
    public List<KiraciKullaniciListItemViewModel> KiraciKullanicilar { get; set; } = [];
    public List<Entities.Davetiye> BekleyenDavetler { get; set; } = [];
}
