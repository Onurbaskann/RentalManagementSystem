using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Roles = RoleNames.SistemYoneticisi)]
[Route("Admin/Roller")]
public class AdminRolController : Controller
{
    private readonly IRolService _rolService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AdminRolController(IRolService rolService, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _rolService = rolService;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var roller = await _rolService.GetInternalRollerAsync();
        var model = new List<RolListeViewModel>();

        foreach (var r in roller)
        {
            var kullaniciSayisi = await _db.UserRoller.CountAsync(ur => ur.RolId == r.Id);
            var perms = await _rolService.GetRolPermissionsAsync(r.Id);
            model.Add(new RolListeViewModel
            {
                Id = r.Id,
                Ad = r.Ad,
                Aciklama = r.Aciklama,
                IsSystemRole = r.IsSystemRole,
                IsActive = r.IsActive,
                KullaniciSayisi = kullaniciSayisi,
                IzinSayisi = perms.Count
            });
        }

        return View(model);
    }

    [HttpGet("Ekle")]
    public IActionResult Create()
    {
        var model = new RolOlusturViewModel();
        PopulatePermissions(model.Permissions, new List<string>());
        return View(model);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RolOlusturViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            var rol = await _rolService.CreateAsync(model.Ad, model.Aciklama, currentUserId);
            await _rolService.SetRolPermissionsAsync(rol.Id, model.SelectedPermissions, currentUserId);
            TempData["Success"] = $"'{model.Ad}' rolü oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }
    }

    [HttpGet("Duzenle/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var rol = await _rolService.GetByIdAsync(id);
        if (rol == null) return NotFound();

        var selected = await _rolService.GetRolPermissionsAsync(id);
        var model = new RolDuzenleViewModel
        {
            Id = rol.Id,
            Ad = rol.Ad,
            Aciklama = rol.Aciklama,
            IsSystemRole = rol.IsSystemRole,
            SelectedPermissions = selected
        };
        PopulatePermissions(model.Permissions, selected);
        return View(model);
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RolDuzenleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _rolService.UpdateAsync(id, model.Ad, model.Aciklama, currentUserId);
            await _rolService.SetRolPermissionsAsync(id, model.SelectedPermissions, currentUserId);
            TempData["Success"] = $"'{model.Ad}' rolü güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }
    }

    [HttpPost("Sil/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUserId = _userManager.GetUserId(User)!;
            await _rolService.SilAsync(id, currentUserId);
            TempData["Success"] = "Rol silindi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static void PopulatePermissions(List<PermissionGrupViewModel> target, List<string> selected)
    {
        target.Clear();
        target.AddRange(
            PermissionCatalog.All
                .GroupBy(p => p.Split('.')[1])
                .Select(g => new PermissionGrupViewModel
                {
                    GrupAdi = GetModuleLabel(g.Key),
                    Permissions = g.Select(p => new PermissionCheckboxViewModel
                    {
                        Value = p,
                        Etiket = GetActionLabel(p.Split('.')[2]),
                        Selected = selected.Contains(p)
                    }).ToList()
                })
        );
    }

    private static string GetModuleLabel(string module) => module switch
    {
        "Tasinmaz"      => "Taşınmaz",
        "Birim"         => "Birim / Ofis",
        "Kiraci"        => "Kiracı",
        "Sozlesme"      => "Sözleşme",
        "Odeme"         => "Ödeme",
        "Kullanici"     => "Kullanıcı",
        "Rol"           => "Rol",
        "Davetiye"      => "Davetiye",
        "Audit"         => "Denetim",
        "BorcTipi"      => "Borç Tipi",
        "ManuelBorc"    => "Manuel Borç",
        "Rezervasyon"   => "Rezervasyon",
        "TasinmazCarpan" => "Taşınmaz Çarpan",
        "Bildirim"      => "Bildirim",
        "Tarife"        => "Tarife",
        "Tahakkuk"      => "Tahakkuk",
        "Parametre"     => "Parametreler",
        _               => module
    };

    private static string GetActionLabel(string action) => action switch
    {
        "View"                 => "Görüntüle",
        "Create"               => "Ekle",
        "Edit"                 => "Düzenle",
        "Delete"               => "Sil",
        "Cancel"               => "İptal Et",
        "Extend"               => "Süre Uzat",
        "Terminate"            => "Feshet",
        "OverrideRate"         => "Elle Müdahale",
        "Approve"              => "Onayla",
        "Reject"               => "Reddet",
        "UploadDekont"         => "Dekont Yükle",
        "ImportBankStatement"  => "Banka Hareketleri İçe Aktar",
        "MatchBankTransaction" => "Banka Hareketi Eşleştir",
        "AssignPermission"     => "Yetki Ata",
        "ManageRate"           => "Tarife Yönet",
        "TransferToTahakkuk"   => "Tahakkuka Aktar",
        "Manage"               => "Yönet",
        "Regenerate"           => "Yeniden Üret",
        "Resend"               => "Yeniden Gönder",
        "BorcHatirlatma"       => "Borç Hatırlatma",
        _                      => action
    };
}
