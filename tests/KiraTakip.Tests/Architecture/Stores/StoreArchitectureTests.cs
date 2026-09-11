using KiraTakip.Application.DependencyInjection;
using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Infrastructure;
using KiraTakip.Domain.Auditing;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KiraTakip.Tests;

public class StoreArchitectureTests
{
    [Fact]
    public void Permissions_ShouldBeInternalAndExcludedFromTenantAndScopeLists()
    {
        var module = Assert.Single(
            PermissionCatalog.AllModules,
            item => item.Path == PermissionCatalog.Store.Module);
        var expected = new[]
        {
            PermissionCatalog.Store.Create,
            PermissionCatalog.Store.Edit,
            PermissionCatalog.Store.Account
        };

        Assert.All(expected, permission => Assert.Contains(permission, module.Actions));
        Assert.Contains(PermissionCatalog.Store.Module, PermissionCatalog.OperasyonMuduruIzinleri);
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.OperasyonMuduruIzinleri));
        Assert.Contains(PermissionCatalog.Store.Module, PermissionCatalog.All);
        Assert.All(expected, permission => Assert.Contains(permission, PermissionCatalog.All));
        Assert.DoesNotContain(PermissionCatalog.Store.Module, PermissionCatalog.ScopeAware);
        Assert.DoesNotContain(PermissionCatalog.Store.Module, PermissionCatalog.TenantAll);
        Assert.All(expected, permission => Assert.DoesNotContain(permission, PermissionCatalog.TenantAll));
    }

    [Fact]
    public void ControllerPostActions_ShouldRequirePermissionAndAntiforgery()
    {
        AssertPostAction(nameof(AdminStoreController.Create), PermissionCatalog.Store.Create, typeof(StoreFormViewModel));
        AssertPostAction(nameof(AdminStoreController.Edit), PermissionCatalog.Store.Edit, typeof(int), typeof(StoreFormViewModel));
        AssertPostAction(nameof(AdminStoreController.ToggleStatus), PermissionCatalog.Store.Edit, typeof(int));
        AssertPostAction(nameof(AdminStoreController.ReplaceAccount), PermissionCatalog.Store.Account, typeof(int), typeof(StoreAccountFormViewModel));
        AssertPostAction(nameof(AdminStoreController.DeactivateAccount), PermissionCatalog.Store.Account, typeof(int), typeof(int));
    }

    [Fact]
    public void ResponseContractsAndEntity_ShouldNotExposePlaintextSecret()
    {
        var responseTypes = new[]
        {
            typeof(StoreListItemDto),
            typeof(StoreDetailDto),
            typeof(StoreAccountHistoryItemDto)
        };
        var forbiddenParts = new[] { "Password", "Protected", "Secret", "Credential" };

        foreach (var type in responseTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), property =>
                forbiddenParts.Any(part => property.Name.Contains(part, StringComparison.OrdinalIgnoreCase)));
        }

        Assert.Null(typeof(StoreAccount).GetProperty("MerchantPassword"));
        var protectedProperty = typeof(StoreAccount).GetProperty(nameof(StoreAccount.ProtectedMerchantPassword));
        Assert.NotNull(protectedProperty);
        Assert.NotNull(protectedProperty!.GetCustomAttribute<AuditIgnoreAttribute>());
    }

    [Fact]
    public void DependencyModules_ShouldRegisterStoreContracts()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddRepositoryModule();
        services.AddApplicationModule();
        services.AddInfrastructureModule(configuration);

        Assert.Contains(services, item => item.ServiceType == typeof(IStoreRepository));
        Assert.Contains(services, item => item.ServiceType == typeof(IStoreAccountRepository));
        Assert.Contains(services, item => item.ServiceType == typeof(IStoreService));
        Assert.Contains(services, item => item.ServiceType == typeof(IStoreAccountCredentialProtector));
    }

    [Fact]
    public void TenantContracts_ShouldNotContainStoreOrMerchantFields()
    {
        var tenantContractTypes = typeof(TenantChargeDetailsViewModel).Assembly
            .GetTypes()
            .Where(type =>
                ((type.Namespace?.StartsWith("KiraTakip.Models", StringComparison.Ordinal) ?? false) ||
                 (type.Namespace?.StartsWith("KiraTakip.Web.Models", StringComparison.Ordinal) ?? false)) &&
                type.Name.StartsWith("Tenant", StringComparison.Ordinal));
        var forbiddenParts = new[] { "Store", "Merchant", "Magaza" };

        foreach (var type in tenantContractTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), property =>
                forbiddenParts.Any(part => property.Name.Contains(part, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static void AssertPostAction(string methodName, string policy, params Type[] parameterTypes)
    {
        var method = typeof(AdminStoreController).GetMethod(methodName, parameterTypes);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(policy, method.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }
}
