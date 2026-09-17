using KiraTakip.Application.DependencyInjection;
using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Models.Dtos.ChargeType;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KiraTakip.Tests;

public class PaymentStoreRoutingArchitectureTests
{
    [Fact]
    public void Permissions_ShouldBeInternalAndExcludedFromTenantAndScopeLists()
    {
        var module = Assert.Single(PermissionCatalog.AllModules,
            item => item.Path == PermissionCatalog.PaymentRouting.Module);
        var expected = new[] { PermissionCatalog.PaymentRouting.Create, PermissionCatalog.PaymentRouting.Edit };

        Assert.All(expected, permission => Assert.Contains(permission, module.Actions));
        Assert.Contains(PermissionCatalog.PaymentRouting.Module, PermissionCatalog.InternalAll);
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.InternalAll));
        Assert.DoesNotContain(PermissionCatalog.PaymentRouting.Module, PermissionCatalog.ScopeAware);
        Assert.DoesNotContain(PermissionCatalog.PaymentRouting.Module, PermissionCatalog.TenantAll);
        Assert.All(expected, permission => Assert.DoesNotContain(permission, PermissionCatalog.TenantAll));
    }

    [Fact]
    public void ControllerPostActions_ShouldRequirePermissionAndAntiforgery()
    {
        AssertPostAction(
            nameof(AdminPaymentStoreRoutingController.Save),
            PermissionCatalog.PaymentRouting.Create,
            typeof(PaymentStoreRoutingFormViewModel));
        AssertPostAction(
            nameof(AdminPaymentStoreRoutingController.Deactivate),
            PermissionCatalog.PaymentRouting.Edit,
            typeof(int));
    }

    [Fact]
    public void DependencyModules_ShouldRegisterRoutingAndResolverContracts()
    {
        var services = new ServiceCollection();
        services.AddRepositoryModule();
        services.AddApplicationModule();

        Assert.Contains(services, item => item.ServiceType == typeof(IPaymentStoreRoutingRepository));
        Assert.Contains(services, item => item.ServiceType == typeof(IPaymentStoreRoutingService));
        Assert.Contains(services, item => item.ServiceType == typeof(IPaymentStoreResolver));
    }

    [Fact]
    public void ResolverContract_ShouldNotExposeMerchantOrSecretAndChargeTypeCreateInputShouldNotControlActiveState()
    {
        var forbidden = new[] { "Merchant", "Password", "Secret", "Credential", "StoreName", "Magaza" };
        Assert.DoesNotContain(typeof(ResolvedPaymentStoreAccountDto).GetProperties(), property =>
            forbidden.Any(part => property.Name.Contains(part, StringComparison.OrdinalIgnoreCase)));
        Assert.Null(typeof(CreateInput).GetProperty("IsActive"));
    }

    [Fact]
    public void Validator_ShouldEnforceExactScopeShape()
    {
        var validator = new PaymentStoreRoutingFormViewModelValidator();

        Assert.True(validator.Validate(new PaymentStoreRoutingFormViewModel
        {
            ChargeTypeId = 1,
            StoreId = 1,
            Scope = PaymentRoutingScope.General
        }).IsValid);
        Assert.False(validator.Validate(new PaymentStoreRoutingFormViewModel
        {
            ChargeTypeId = 1,
            StoreId = 1,
            Scope = PaymentRoutingScope.Unit,
            PropertyId = 1,
            UnitId = 2
        }).IsValid);
    }

    private static void AssertPostAction(string methodName, string policy, params Type[] parameterTypes)
    {
        var method = typeof(AdminPaymentStoreRoutingController).GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(policy, method.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }
}
