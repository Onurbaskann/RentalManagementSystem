using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class RolListeViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int KullaniciSayisi { get; set; }
    public int IzinSayisi { get; set; }
}

public class RolOlusturViewModel
{
    [Required(ErrorMessage = "Rol adı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Rol adı en fazla 100 karakter olabilir.")]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGrupViewModel> Permissions { get; set; } = [];
}

public class RolDuzenleViewModel
{
    public int Id { get; set; }
    public bool IsSystemRole { get; set; }

    [Required(ErrorMessage = "Rol adı zorunludur.")]
    [MaxLength(100)]
    public string Ad { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Aciklama { get; set; }

    public List<string> SelectedPermissions { get; set; } = [];
    public List<PermissionGrupViewModel> Permissions { get; set; } = [];
}
