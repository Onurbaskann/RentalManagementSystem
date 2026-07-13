using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class KiraciKullaniciListeViewModel
{
    public List<KiraciKullaniciItem> Kullanicilar { get; set; } = [];
    public List<KiraciDavetItem> BekleyenDavetler { get; set; } = [];
    public bool CanInvite { get; set; }
    public bool CanManage { get; set; }
}

public class KiraciKullaniciItem
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RolAd { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class KiraciDavetItem
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AdSoyad { get; set; }
    public string RolAd { get; set; } = string.Empty;
    public DateTime GonderimTarihi { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class KiraciDavetViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    public string? AdSoyad { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rol seçilmelidir.")]
    public int RolId { get; set; }

    public List<int> BirimIds { get; set; } = [];
    public List<RolSecenekViewModel> Roller { get; set; } = [];
    public List<UnitLookupDto> Units { get; set; } = [];
}

public class KiraciKullaniciDuzenleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Rol seçilmelidir.")]
    public int RolId { get; set; }

    public List<RolSecenekViewModel> Roller { get; set; } = [];
}
