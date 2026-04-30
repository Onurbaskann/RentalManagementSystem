using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;

namespace KiraTakip.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Kullanicilar")]
public class AdminUserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly DummyDataService _data;
    private readonly UserTasinmazYetkiService _yetkiService;

    public AdminUserController(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        DummyDataService data,
        UserTasinmazYetkiService yetkiService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _data = data;
        _yetkiService = yetkiService;
    }

    private static readonly string[] Roller = ["Admin", "Yonetici", "Goruntuleyici"];

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
    public IActionResult Create()
    {
        ViewBag.Roller = Roller;
        var model = new KullaniciEkleViewModel();
        PopulateTasinmazlar(model.Tasinmazlar, new List<int>());
        return View(model);
    }

    [HttpPost("Ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KullaniciEkleViewModel model)
    {
        ViewBag.Roller = Roller;

        if (!ModelState.IsValid)
        {
            PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        if (!Roller.Contains(model.Rol))
        {
            ModelState.AddModelError("Rol", "Geçersiz rol seçildi.");
            PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
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
            
            PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Rol);

        if (model.Rol == "Goruntuleyici" && model.SelectedTasinmazIds != null && model.SelectedTasinmazIds.Any())
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

        ViewBag.Roller = Roller;
        var model = new KullaniciDuzenleViewModel
        {
            Id = user.Id,
            AdSoyad = user.AdSoyad ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Rol = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive,
            IsCurrentUser = user.Id == currentUserId,
            SelectedTasinmazIds = yetkiliIds
        };
        
        PopulateTasinmazlar(model.Tasinmazlar, yetkiliIds);
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

        if (!ModelState.IsValid)
        {
            PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        if (!Roller.Contains(model.Rol))
        {
            ModelState.AddModelError("Rol", "Geçersiz rol seçildi.");
            PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
            return View(model);
        }

        // Admin kendi rolünü değiştiremez
        if (user.Id == currentUserId)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.FirstOrDefault() != model.Rol)
            {
                ModelState.AddModelError("Rol", "Kendi rolünüzü değiştiremezsiniz.");
                PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
                return View(model);
            }
        }

        // Son aktif Admin koruması (rol Admin'den başka bir role çekiliyorsa)
        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Contains("Admin") && model.Rol != "Admin")
        {
            if (await AktifAdminSayisi() <= 1)
            {
                ModelState.AddModelError("Rol", "Sistemde en az bir aktif Admin bulunmalıdır.");
                PopulateTasinmazlar(model.Tasinmazlar, model.SelectedTasinmazIds);
                return View(model);
            }
        }

        // Rol güncelle
        await _userManager.RemoveFromRolesAsync(user, existingRoles);
        await _userManager.AddToRoleAsync(user, model.Rol);

        if (model.Rol == "Goruntuleyici")
        {
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, model.SelectedTasinmazIds, currentUserId ?? "system");
        }
        else
        {
            await _yetkiService.SetUserTasinmazYetkileriAsync(user.Id, new List<int>(), currentUserId ?? "system");
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

        // Admin kendi hesabını pasif yapamaz
        if (user.Id == currentUserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        // Son aktif Admin koruması
        if (user.IsActive)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") && await AktifAdminSayisi() <= 1)
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
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.Count(u => u.IsActive);
    }

    private void PopulateTasinmazlar(List<TasinmazYetkiCheckboxViewModel> tasinmazlarListesi, List<int> selectedIds)
    {
        tasinmazlarListesi.Clear();
        foreach (var t in _data.Tasinmazlar)
        {
            tasinmazlarListesi.Add(new TasinmazYetkiCheckboxViewModel
            {
                TasinmazId = t.Id,
                Ad = t.Ad,
                Konum = $"{t.Il} / {t.Ilce}",
                Selected = selectedIds?.Contains(t.Id) ?? false
            });
        }
    }
}
