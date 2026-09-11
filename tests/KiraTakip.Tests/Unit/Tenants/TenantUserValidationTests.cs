using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.Extensions;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Tenants;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using KiraTakip.Models.Dtos.TenantUser;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

public class TenantUserValidationTests
{
    [Fact]
    public void InvitationValidator_ShouldRejectInvalidLengthsRoleAndUnits()
    {
        var result = new TenantInvitationFormViewModelValidator().Validate(
            new TenantInvitationFormViewModel
            {
                Email = new string('a', 250) + "@test.com",
                FullName = new string('a', 201),
                RoleId = 0,
                UnitIds = [1, 1]
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantInvitationFormViewModel.Email));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantInvitationFormViewModel.FullName));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantInvitationFormViewModel.RoleId));
        Assert.Contains(result.Errors, error => error.Field == nameof(TenantInvitationFormViewModel.UnitIds));
    }

    [Fact]
    public void EditValidator_ShouldUseProjectIValidatorInfrastructure()
    {
        var result = new TenantUserEditViewModelValidator().Validate(
            new TenantUserEditViewModel
            {
                RoleId = 0,
                HasAccessToAllUnits = true
            });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors, error =>
            error.Field == nameof(TenantUserEditViewModel.RoleId));
    }

    [Fact]
    public void EditValidator_ShouldRequireAUniqueUnitWhenGlobalAccessIsDisabled()
    {
        var noUnitResult = new TenantUserEditViewModelValidator().Validate(
            new TenantUserEditViewModel
            {
                RoleId = 1,
                HasAccessToAllUnits = false
            });
        var duplicateResult = new TenantUserEditViewModelValidator().Validate(
            new TenantUserEditViewModel
            {
                RoleId = 1,
                HasAccessToAllUnits = false,
                UnitIds = [10, 10]
            });

        Assert.Contains(noUnitResult.Errors, error =>
            error.Field == nameof(TenantUserEditViewModel.UnitIds));
        Assert.Contains(duplicateResult.Errors, error =>
            error.Field == nameof(TenantUserEditViewModel.UnitIds));
    }

    [Fact]
    public async Task Service_ShouldGuardSelfEditAndDeactivationBeforeRepositoryAccess()
    {
        var service = CreateService();
        var editException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantUserForEditAsync(
                new GetTenantUserForEditInput(
                    1,
                    "current-user",
                    "current-user",
                    new ReservationAccessScopeInput())));
        var toggleException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ToggleUserActiveAsync(
                new ToggleTenantUserActiveInput(1, "current-user", "current-user")));

        Assert.Equal("TENANT_USER_SELF_EDIT", editException.Code);
        Assert.Equal("TENANT_USER_SELF_DEACTIVATION", toggleException.Code);
        Assert.IsAssignableFrom<ITransactionalService>(service);
    }

    [Fact]
    public void AdminEditActions_ShouldRequireSystemUserEditPermission()
    {
        var actions = typeof(AdminTenantUserController)
            .GetMethods()
            .Where(method => method.Name == nameof(AdminTenantUserController.Edit))
            .ToList();

        Assert.Equal(2, actions.Count);
        Assert.All(actions, action =>
        {
            var authorize = Assert.Single(action.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>());
            Assert.Equal(PermissionCatalog.User.Edit, authorize.Policy);
        });
        Assert.Contains(actions, action => action.GetCustomAttributes(typeof(HttpGetAttribute), true).Any());
        Assert.Contains(actions, action => action.GetCustomAttributes(typeof(HttpPostAttribute), true).Any());
    }

    [Fact]
    public void TenantAndAdminEditViews_ShouldUseTheSharedScopeForm()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        var viewsRoot = Directory.Exists(Path.Combine(repoRoot, "src", "KiraTakip.Web", "Views"))
            ? Path.Combine(repoRoot, "src", "KiraTakip.Web", "Views")
            : Path.Combine(repoRoot, "Views");
        var tenantView = File.ReadAllText(Path.Combine(viewsRoot, "TenantUser", "Edit.cshtml"));
        var adminView = File.ReadAllText(Path.Combine(viewsRoot, "AdminTenantUser", "Edit.cshtml"));
        var sharedForm = File.ReadAllText(Path.Combine(viewsRoot, "Shared", "_TenantUserEditFields.cshtml"));

        Assert.Contains("_TenantUserEditFields", tenantView);
        Assert.Contains("_TenantUserEditFields", adminView);
        Assert.Contains("Tüm Birimlere Erişim", sharedForm);
        Assert.Contains("Rezervasyon Birimleri", sharedForm);
    }

    [Fact]
    public void SuperAdmin_ShouldPassSystemUserEditVisibilityCheck()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("IsSuperAdmin", "true")
        ], "Test"));

        Assert.True(user.HasPermission(PermissionCatalog.User.Edit));
    }

    private static TenantUserService CreateService()
    {
        return new(null!, null!, null!, null!, null!, null!, null!,
                   null!, null!, null!, null!, null!, null!, null!);
    }
}
