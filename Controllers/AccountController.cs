using KiraTakip.Infrastructure;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Models.Dtos.PasswordReset;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IAuditService auditService,
    IInvitationService invitationService,
    IPasswordResetService passwordResetService,
    ITenantService tenantService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(HomeController.Index), "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [SuppressAutomaticSuccessFeedback]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız pasif durumdadır. Lütfen yöneticinizle iletişime geçin.");
            await auditService.LogAsync("User.LoginFailed", "ApplicationUser", user.Id, "Pasif hesap");
            return View(model);
        }

        // Kiracı kullanıcısı → bağlı kiracının aktif olup olmadığını kontrol et
        if (user != null && user.UserType == UserType.Tenant && user.TenantId.HasValue)
        {
            var tenantInactive = await tenantService.IsInactiveAsync(
                new CheckTenantInactiveInput(user.TenantId.Value));
            if (tenantInactive)
            {
                ModelState.AddModelError(string.Empty, "Firmanızın hesabı pasif durumdadır. Lütfen yöneticinizle iletişime geçin.");
                await auditService.LogAsync("User.LoginFailed", "ApplicationUser", user.Id, "Pasif kiracı");
                return View(model);
            }
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await auditService.LogAsync("User.LoginSuccess", "ApplicationUser", user?.Id);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            // Kiracı kullanıcıları kendi paneline yönlendirilir
            if (user?.UserType == UserType.Tenant)
                return RedirectToAction(nameof(TenantPanelController.Index), "TenantPanel");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        if (result.IsLockedOut)
        {
            if (user != null)
                await auditService.LogAsync("User.LockedOut", "ApplicationUser", user.Id);
            ModelState.AddModelError(string.Empty, "Hesabınız çok fazla başarısız giriş denemesi nedeniyle geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(model);
        }

        await auditService.LogAsync("User.LoginFailed", "ApplicationUser", user?.Id);
        ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SuppressAutomaticSuccessFeedback]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        await auditService.LogAsync("User.Logout", "ApplicationUser", userId);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        await auditService.LogAsync("User.PasswordChanged", "ApplicationUser", user.Id);
        return RedirectToAction(nameof(ChangePassword));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Invite(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var (success, error, invitation) = await invitationService.ValidateAsync(token);
        if (!success || invitation == null)
        {
            ViewBag.ErrorMessage = error ?? "Geçersiz veya süresi dolmuş davet bağlantısı.";
            return View("InviteError");
        }

        var model = new InviteAcceptViewModel
        {
            Token = token,
            Email = invitation.Email,
            FullName = invitation.FullName ?? string.Empty
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(InviteAcceptViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, invitation) = await invitationService.ValidateAsync(model.Token);
        if (!success || invitation == null)
        {
            ViewBag.ErrorMessage = error ?? "Geçersiz veya süresi dolmuş davet bağlantısı.";
            return View("InviteError");
        }

        try
        {
            var user = await invitationService.AcceptAsync(invitation, new AcceptInput(model.FullName, model.Password));
            await signInManager.SignInAsync(user, isPersistent: false);
            if (user.UserType == UserType.Tenant)
                return RedirectToAction(nameof(TenantPanelController.Index), "TenantPanel");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await passwordResetService.RequestAsync(new RequestInput(model.Email, ip));

        ViewBag.IsSent = true;
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var (success, error, _) = await passwordResetService.ValidateAsync(token);
        if (!success)
        {
            ViewBag.ErrorMessage = error ?? "Geçersiz veya süresi dolmuş bağlantı.";
            return View("ResetPasswordError");
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, resetRequest) = await passwordResetService.ValidateAsync(model.Token);
        if (!success || resetRequest == null)
        {
            ViewBag.ErrorMessage = error ?? "Geçersiz veya süresi dolmuş bağlantı.";
            return View("ResetPasswordError");
        }

        var changed = await passwordResetService.ResetPasswordAsync(resetRequest, new ResetPasswordInput(model.Password));
        if (!changed)
        {
            ModelState.AddModelError(string.Empty, "Şifre değiştirilemedi. Lütfen tekrar deneyin.");
            return View(model);
        }

        ViewBag.IsSuccess = true;
        return View(model);
    }
}
