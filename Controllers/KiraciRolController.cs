using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Route("Kiraci/Roller")]
public class KiraciRolController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRolService _rolService;
    private readonly IKiraciKullaniciService _kullaniciService;

    public KiraciRolController(
        ApplicationDbContext db,
        ICurrentUserContext currentUser,
        UserManager<ApplicationUser> userManager,
        IRolService rolService,
        IKiraciKullaniciService kullaniciService)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
        _rolService = rolService;
        _kullaniciService = kullaniciService;
    }

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Module)]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var roller = await _rolService.GetKiraciRollerAsync(kiraciId);

        var model = new List<RolListeViewModel>();
        foreach (var r in roller)
        {
            var kullaniciSayisi = await _db.UserRoller
                .CountAsync(ur => ur.RolId == r.Id &&
                                  _db.Users.Any(u => u.Id == ur.UserId && u.KiraciId == kiraciId));
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
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Create)]
    public IActionResult Create()
    {
        var model = new RolOlusturViewModel();
        PopulateKiraciPermissions(model.Permissions, []);
        return View(model);
    }

    [HttpPost("Ekle")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RolOlusturViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateKiraciPermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        var kiraciId = _currentUser.KiraciId!.Value;
        var currentUserId = _userManager.GetUserId(User)!;

        try
        {
            var rol = await _db.Roller
                .Where(r => r.KiraciId == kiraciId && r.Ad == model.Ad && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (rol != null)
            {
                ModelState.AddModelError("Ad", "Bu isimde bir rol zaten var.");
                PopulateKiraciPermissions(model.Permissions, model.SelectedPermissions);
                return View(model);
            }

            var yeniRol = new Rol
            {
                Ad = model.Ad,
                Aciklama = model.Aciklama,
                Scope = Models.RolScope.Kiraci,
                KiraciId = kiraciId,
                IsSystemRole = false,
                IsActive = true
            };
            _db.Roller.Add(yeniRol);
            await _db.SaveChangesAsync();

            var validPerms = model.SelectedPermissions
                .Where(p => PermissionCatalog.KiraciAll.Contains(p))
                .ToList();
            await _rolService.SetRolPermissionsAsync(yeniRol.Id, validPerms, currentUserId);

            TempData["Success"] = $"'{model.Ad}' rolü oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            PopulateKiraciPermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }
    }

    [HttpGet("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var rol = await _db.Roller
            .FirstOrDefaultAsync(r => r.Id == id && r.KiraciId == kiraciId && !r.IsDeleted);
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
        PopulateKiraciPermissions(model.Permissions, selected);
        return View(model);
    }

    [HttpPost("Duzenle/{id:int}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RolDuzenleViewModel model)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var rol = await _db.Roller
            .FirstOrDefaultAsync(r => r.Id == id && r.KiraciId == kiraciId && !r.IsDeleted);
        if (rol == null) return NotFound();

        if (!ModelState.IsValid)
        {
            PopulateKiraciPermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        var removingManage = !model.SelectedPermissions.Contains(PermissionCatalog.KiraciPortal.System.Kullanici.Invite);
        if (removingManage)
        {
            try
            {
                await _kullaniciService.EnsureSonYetkiliAsync(kiraciId, excludeRolId: id);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateKiraciPermissions(model.Permissions, model.SelectedPermissions);
                return View(model);
            }
        }

        var currentUserId = _userManager.GetUserId(User)!;

        if (!rol.IsSystemRole)
        {
            rol.Ad = model.Ad;
            rol.Aciklama = model.Aciklama;
        }

        var validPerms = model.SelectedPermissions
            .Where(p => PermissionCatalog.KiraciAll.Contains(p))
            .ToList();
        await _rolService.SetRolPermissionsAsync(id, validPerms, currentUserId);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{rol.Ad}' rolü güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Sil/{id:int}")]
    [Authorize(Policy = PermissionCatalog.KiraciPortal.System.Rol.Delete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var rol = await _db.Roller
            .FirstOrDefaultAsync(r => r.Id == id && r.KiraciId == kiraciId && !r.IsDeleted);
        if (rol == null) return NotFound();

        if (rol.IsSystemRole)
        {
            TempData["Error"] = "Sistem rolleri silinemez.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _kullaniciService.EnsureSonYetkiliAsync(kiraciId, excludeRolId: id);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = _userManager.GetUserId(User)!;
        try
        {
            await _rolService.SilAsync(id, currentUserId);
            TempData["Success"] = "Rol silindi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static void PopulateKiraciPermissions(List<PermissionGrupViewModel> target, List<string> selected)
    {
        target.Clear();
        target.AddRange(
            PermissionCatalog.AllModules
                .Where(m => m.Path.StartsWith("Kiraci."))
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
        "Create"     => "Ekle",
        "Edit"       => "Düzenle",
        "Delete"     => "Sil",
        "Cancel"     => "İptal Et",
        "Invite"     => "Davet Et",
        "Deactivate" => "Pasifleştir",
        _            => action
    };
}
