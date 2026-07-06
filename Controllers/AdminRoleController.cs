using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "System.Role")]
[Route("Admin/Roller")]
public class AdminRoleController : Controller
{
    private readonly IRoleService _rolService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AdminRoleController(IRoleService roleService, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _rolService = roleService;
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
            PermissionCatalog.AllModules
                .Where(m => !m.Path.StartsWith("Tenant."))
                .Select(m =>
                {
                    var items = new List<PermissionCheckboxViewModel>
                    {
                        new() { Value = m.Path, Etiket = "Görüntüle", Selected = selected.Contains(m.Path) }
                    };
                    items.AddRange(m.Actions.Select(a => new PermissionCheckboxViewModel
                    {
                        Value = a,
                        Etiket = GetActionLabel(a.Split('.').Last()),
                        Selected = selected.Contains(a)
                    }));
                    return new PermissionGrupViewModel { GrupAdi = m.DisplayName, Permissions = items };
                })
        );
    }

    private static string GetActionLabel(string action) => action switch
    {
        "Create"               => "Ekle",
        "Edit"                 => "Düzenle",
        "Delete"               => "Sil",
        "Cancel"               => "İptal Et",
        "Extend"               => "Süre Uzat",
        "Terminate"            => "Feshet",
        "OverrideRate"         => "Elle Müdahale",
        "Approve"              => "Onayla",
        "Reject"               => "Reddet",
        "UploadReceipt"         => "Dekont Yükle",
        "ImportBankStatement"  => "Banka Hareketleri İçe Aktar",
        "MatchBankTransaction" => "Banka Hareketi Eşleştir",
        "AssignPermission"     => "Yetki Ata",
        "TransferToCharge"   => "Tahakkuka Aktar",
        "Regenerate"           => "Yeniden Üret",
        "Resend"               => "Yeniden Gönder",
        "BorcHatirlatma"       => "Borç Hatırlatma",
        _                      => action
    };
}
