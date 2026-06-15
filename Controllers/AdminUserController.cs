using KiraTakip.Authorization;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Roles = RoleNames.Admin)]
[Route("Admin/Kullanicilar")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITasinmazService _tasinmazService;
    private readonly IUserTasinmazYetkiService _yetkiService;
    private readonly IPermissionService _permissionService;

    public AdminUserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITasinmazService tasinmazService,
        IUserTasinmazYetkiService yetkiService,
        IPermissionService permissionService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tasinmazService = tasinmazService;
        _yetkiService = yetkiService;
        _permissionService = permissionService;
    }

    private static readonly string[] Roller = [RoleNames.Admin, RoleNames.Yonetici, RoleNames.Goruntuleyici];

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.AdSoyad).ToListAsync();
        var model = new List<KullaniciListeViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            model.Add(new KullaniciListeViewModel
            {
                Id = u.Id,
                AdSoyad = u.AdSoyad ?? u.Email ?? "—",
                Email = u.Email ?? "—",
                Rol = roles.FirstOrDefault() ?? "—",
                IsActive = u.IsActive
            });
        }

        return View(model);
    }

    [HttpGet("Ekle")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roller = Roller;
        var model = new KullaniciEkleViewModel();
        await PopulateTasinmazlarAsync(model.Tasinmazlar, new List<int>());
        return View(model);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KullaniciEkleViewModel model)
    {
        ViewBag.Roller = Roller;

        if (string.IsNullOrWhiteSpace(model.AdSoyad))
            ModelState.AddModelError("AdSoyad", "Ad soyad zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError("Email", "E-posta adresi zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError("Password", "Şifre zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Rol))
            ModelState.AddModelError("Rol", "Rol seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        if (!Roller.Contains(model.Rol))
        {
            ModelState.AddModelError("Rol", "Geçersiz rol seçildi.");
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            AdSoyad = model.AdSoyad,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);

            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Rol);

        if (model.Rol == RoleNames.Goruntuleyici && model.SelectedTasinmazIds != null && model.SelectedTasinmazIds.Any())
        {
            var currentUserId = _userManager.GetUserId(User);
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, model.SelectedTasinmazIds, currentUserId ?? "system");
        }

        TempData["Success"] = $"{model.AdSoyad} kullanıcısı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Duzenle/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var roles = await _userManager.GetRolesAsync(user);
        var yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(user.Id);
        var mevcutPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);

        ViewBag.Roller = Roller;
        var model = new KullaniciDuzenleViewModel
        {
            Id = user.Id,
            AdSoyad = user.AdSoyad ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Rol = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive,
            IsCurrentUser = user.Id == currentUserId,
            SelectedTasinmazIds = yetkiliIds,
            SelectedPermissions = mevcutPermissions.ToList()
        };

        await PopulateTasinmazlarAsync(model.Tasinmazlar, yetkiliIds);
        PopulatePermissions(model);
        return View(model);
    }

    [HttpPost("Duzenle/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, KullaniciDuzenleViewModel model)
    {
        ViewBag.Roller = Roller;

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        model.IsCurrentUser = user.Id == currentUserId;

        if (string.IsNullOrWhiteSpace(model.Rol))
            ModelState.AddModelError("Rol", "Rol seçilmelidir.");

        if (!ModelState.IsValid)
        {
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            PopulatePermissions(model);
            return View(model);
        }

        if (!Roller.Contains(model.Rol))
        {
            ModelState.AddModelError("Rol", "Geçersiz rol seçildi.");
            await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
            PopulatePermissions(model);
            return View(model);
        }

        if (user.Id == currentUserId)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.FirstOrDefault() != model.Rol)
            {
                ModelState.AddModelError("Rol", "Kendi rolünüzü değiştiremezsiniz.");
                await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
                PopulatePermissions(model);
                return View(model);
            }
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Contains(RoleNames.Admin) && model.Rol != RoleNames.Admin)
        {
            if (await AktifAdminSayisi() <= 1)
            {
                ModelState.AddModelError("Rol", "Sistemde en az bir aktif Admin bulunmalıdır.");
                await PopulateTasinmazlarAsync(model.Tasinmazlar, model.SelectedTasinmazIds);
                PopulatePermissions(model);
                return View(model);
            }
        }

        await _userManager.RemoveFromRolesAsync(user, existingRoles);
        await _userManager.AddToRoleAsync(user, model.Rol);

        var selectedPerms = model.SelectedPermissions ?? new List<string>();

        if (model.Rol == RoleNames.Admin)
        {
            await _permissionService.SetUserPermissionsAsync(user.Id, Array.Empty<string>(), currentUserId ?? "system");
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, new List<int>(), currentUserId ?? "system");
        }
        else if (model.Rol == RoleNames.Yonetici)
        {
            var allowed = selectedPerms.Where(p => PermissionCatalog.AssignableToYonetici.Contains(p)).ToList();
            await _permissionService.SetUserPermissionsAsync(user.Id, allowed, currentUserId ?? "system");
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, new List<int>(), currentUserId ?? "system");
        }
        else if (model.Rol == RoleNames.Goruntuleyici)
        {
            var allowed = selectedPerms.Where(p => PermissionCatalog.AssignableToGoruntuleyici.Contains(p)).ToList();
            await _permissionService.SetUserPermissionsAsync(user.Id, allowed, currentUserId ?? "system");
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, model.SelectedTasinmazIds, currentUserId ?? "system");
        }

        user.AdSoyad = model.AdSoyad;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"{user.AdSoyad ?? user.Email} kullanıcısı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("DurumDegistir/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        if (user.IsActive)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleNames.Admin) && await AktifAdminSayisi() <= 1)
            {
                TempData["Error"] = "Sistemde en az bir aktif Admin bulunmalıdır.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"{user.AdSoyad ?? user.Email} {(user.IsActive ? "aktif" : "pasif")} hale getirildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> AktifAdminSayisi()
    {
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        return admins.Count(u => u.IsActive);
    }

    private async Task PopulateTasinmazlarAsync(List<TasinmazYetkiCheckboxViewModel> liste, List<int> selectedIds)
    {
        liste.Clear();
        var tasinmazlar = await _tasinmazService.GetAllAsync();
        foreach (var t in tasinmazlar)
        {
            liste.Add(new TasinmazYetkiCheckboxViewModel
            {
                TasinmazId = t.Id,
                Ad = t.Ad,
                Konum = $"{t.Il} / {t.Ilce}",
                Selected = selectedIds?.Contains(t.Id) ?? false
            });
        }
    }

    private static void PopulatePermissions(KullaniciDuzenleViewModel model)
    {
        var selected = model.SelectedPermissions ?? new List<string>();

        model.YoneticiPermissions = PermissionCatalog.AssignableToYonetici
            .GroupBy(p => p.Split('.')[0])
            .Select(g => new PermissionGrupViewModel
            {
                GrupAdi = GetModuleLabel(g.Key),
                Permissions = g.Select(p => new PermissionCheckboxViewModel
                {
                    Value = p,
                    Etiket = GetActionLabel(p.Split('.')[1]),
                    Selected = selected.Contains(p)
                }).ToList()
            })
            .ToList();

        model.GoruntuleyiciPermissions = PermissionCatalog.AssignableToGoruntuleyici
            .Select(p => new PermissionCheckboxViewModel
            {
                Value = p,
                Etiket = $"{GetModuleLabel(p.Split('.')[0])} — Görüntüle",
                Selected = selected.Contains(p)
            })
            .ToList();
    }

    private static string GetModuleLabel(string module) => module switch
    {
        "Tasinmaz" => "Taşınmaz",
        "Birim"    => "Birim / Ofis",
        "Kiraci"   => "Kiracı",
        "Sozlesme" => "Sözleşme",
        "Odeme"    => "Ödeme",
        "Kullanici" => "Kullanıcı Yönetimi",
        _          => module
    };

    private static string GetActionLabel(string action) => action switch
    {
        "View"                => "Görüntüle",
        "Create"              => "Ekle",
        "Edit"                => "Düzenle",
        "Extend"              => "Süre Uzat",
        "Terminate"           => "Feshet",
        "OverrideRate"        => "Elle Müdahale",
        "Approve"             => "Onayla",
        "Reject"              => "Reddet",
        "UploadDekont"        => "Dekont Yükle",
        "ImportBankStatement" => "Banka Hareketleri İçe Aktar",
        "MatchBankTransaction" => "Banka Hareketi Eşleştir",
        "AssignPermission"    => "Yetki Ata",
        _                     => action
    };
}
