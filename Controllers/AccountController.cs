using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IDavetiyeService _davetiyeService;
    private readonly ISifreSifirlamaService _sifreSifirlamaService;
    private readonly ApplicationDbContext _db;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        IDavetiyeService davetiyeService,
        ISifreSifirlamaService sifreSifirlamaService,
        ApplicationDbContext db)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _davetiyeService = davetiyeService;
        _sifreSifirlamaService = sifreSifirlamaService;
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError("Email", "E-posta adresi zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError("Password", "Şifre zorunludur.");

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız pasif durumdadır. Lütfen yöneticinizle iletişime geçin.");
            await _auditService.LogAsync("User.LoginFailed", "ApplicationUser", user.Id, "Pasif hesap");
            return View(model);
        }

        // Kiracı kullanıcısı → bağlı kiracının aktif olup olmadığını kontrol et
        if (user != null && user.UserType == UserType.Tenant && user.KiraciId.HasValue)
        {
            var kiraci = await _db.Kiraciler.IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Id == user.KiraciId.Value);
            if (kiraci != null && !kiraci.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Firmanızın hesabı pasif durumdadır. Lütfen yöneticinizle iletişime geçin.");
                await _auditService.LogAsync("User.LoginFailed", "ApplicationUser", user.Id, "Pasif kiracı");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("User.LoginSuccess", "ApplicationUser", user?.Id);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            // Kiracı kullanıcıları kendi paneline yönlendirilir
            if (user?.UserType == UserType.Tenant)
                return RedirectToAction("Index", "KiraciPanel");
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            if (user != null)
                await _auditService.LogAsync("User.LockedOut", "ApplicationUser", user.Id);
            ModelState.AddModelError(string.Empty, "Hesabınız çok fazla başarısız giriş denemesi nedeniyle geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(model);
        }

        await _auditService.LogAsync("User.LoginFailed", "ApplicationUser", user?.Id);
        ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        await _auditService.LogAsync("User.Logout", "ApplicationUser", userId);
        return RedirectToAction("Login");
    }

    [HttpGet]
    [Authorize]
    public IActionResult SifreDegistir() => View(new SifreDegistirViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreDegistir(SifreDegistirViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, model.MevcutSifre, model.YeniSifre);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _auditService.LogAsync("User.PasswordChanged", "ApplicationUser", user.Id);
        TempData["Success"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToAction(nameof(SifreDegistir));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Davet(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var (success, error, davetiye) = await _davetiyeService.DogrulaAsync(token);
        if (!success || davetiye == null)
        {
            ViewBag.Hata = error ?? "Geçersiz veya süresi dolmuş davet bağlantısı.";
            return View("DavetHata");
        }

        var model = new DavetKabulViewModel
        {
            Token = token,
            Email = davetiye.Email,
            AdSoyad = davetiye.AdSoyad ?? string.Empty
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Davet(DavetKabulViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, davetiye) = await _davetiyeService.DogrulaAsync(model.Token);
        if (!success || davetiye == null)
        {
            ViewBag.Hata = error ?? "Geçersiz veya süresi dolmuş davet bağlantısı.";
            return View("DavetHata");
        }

        try
        {
            var user = await _davetiyeService.KabulEtAsync(davetiye, model.AdSoyad, model.Password);
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = "Hesabınız başarıyla oluşturuldu. Hoş geldiniz!";
            if (user.UserType == UserType.Tenant)
                return RedirectToAction("Index", "KiraciPanel");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SifreUnuttum()
    {
        return View(new SifreUnuttumViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreUnuttum(SifreUnuttumViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _sifreSifirlamaService.TalepOlusturAsync(model.Email, ip);

        ViewBag.Gonderildi = true;
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SifreSifirla(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var (success, error, _) = await _sifreSifirlamaService.DogrulaAsync(token);
        if (!success)
        {
            ViewBag.Hata = error ?? "Geçersiz veya süresi dolmuş bağlantı.";
            return View("SifreSifirlaHata");
        }

        return View(new SifreSifirlaViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreSifirla(SifreSifirlaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, talep) = await _sifreSifirlamaService.DogrulaAsync(model.Token);
        if (!success || talep == null)
        {
            ViewBag.Hata = error ?? "Geçersiz veya süresi dolmuş bağlantı.";
            return View("SifreSifirlaHata");
        }

        var degisti = await _sifreSifirlamaService.SifreDegistirAsync(talep, model.Password);
        if (!degisti)
        {
            ModelState.AddModelError(string.Empty, "Şifre değiştirilemedi. Lütfen tekrar deneyin.");
            return View(model);
        }

        ViewBag.Basarili = true;
        return View(model);
    }
}
