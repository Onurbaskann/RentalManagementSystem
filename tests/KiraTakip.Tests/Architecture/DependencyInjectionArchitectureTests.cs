using System;
using System.Linq;
using KiraTakip.Application.DependencyInjection;
using KiraTakip.Common;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Infrastructure.Identity;
using KiraTakip.Infrastructure.Notifications;
using KiraTakip.Infrastructure.Seeding;
using KiraTakip.Web.Context;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Web.Identity;
using KiraTakip.Web.Rendering;
using KiraTakip.Services.Auditing;
using KiraTakip.Services.Banking;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Documents;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Banking;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Notifications;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Reporting;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Security;
using KiraTakip.Services.Interfaces.Settings;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Notifications;
using KiraTakip.Services.Payments;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Properties;
using KiraTakip.Services.Reporting;
using KiraTakip.Services.Reservations;
using KiraTakip.Services.Security;
using KiraTakip.Services.Settings;
using KiraTakip.Services.Tenants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KiraTakip.Tests.Architecture;

public class DependencyInjectionArchitectureTests
{
    [Theory]
    [InlineData(typeof(IMaskingService), typeof(MaskingService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPermissionService), typeof(PermissionService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPermissionScopeProvider), typeof(PermissionScopeProvider), ServiceLifetime.Scoped)]
    [InlineData(typeof(IUserRoleService), typeof(UserRoleService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRoleService), typeof(RoleService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IAdminUserService), typeof(AdminUserService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ITenantUserService), typeof(TenantUserService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IInvitationService), typeof(InvitationService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPasswordResetService), typeof(PasswordResetService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IAuditService), typeof(AuditService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPropertyService), typeof(PropertyService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IUnitService), typeof(UnitService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPropertyTypeService), typeof(PropertyTypeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IUnitTypeService), typeof(UnitTypeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ITenantService), typeof(TenantService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ISectorService), typeof(SectorService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ITenantCategoryService), typeof(TenantCategoryService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ITenantPanelService), typeof(TenantPanelService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ILeaseService), typeof(LeaseService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IReservationService), typeof(ReservationService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IReservationCompletionService), typeof(ReservationCompletionService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IChargeService), typeof(ChargeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IChargeGenerationService), typeof(ChargeGenerationService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IManualChargeService), typeof(ManualChargeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IChargeTypeService), typeof(ChargeTypeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IChargeReminderService), typeof(ChargeReminderService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRateResolverService), typeof(RateResolverService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPropertyPricingService), typeof(PropertyPricingService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IUnitPricingService), typeof(UnitPricingService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRateHierarchyService), typeof(RateHierarchyService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRateScheduleService), typeof(RateScheduleService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPaymentService), typeof(PaymentService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IStoreService), typeof(StoreService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPaymentStoreRoutingService), typeof(PaymentStoreRoutingService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IPaymentStoreResolver), typeof(PaymentStoreResolver), ServiceLifetime.Scoped)]
    [InlineData(typeof(IOnlinePaymentService), typeof(OnlinePaymentService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IBankTransactionService), typeof(BankTransactionService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IDocumentService), typeof(DocumentService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IDocumentTypeService), typeof(DocumentTypeService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IStatisticsService), typeof(StatisticsService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ISystemSettingService), typeof(SystemSettingService), ServiceLifetime.Scoped)]
    public void AddApplicationModule_ShouldRegisterAllApplicationServicesWithCorrectLifetime(
        Type serviceType, Type implementationType, ServiceLifetime expectedLifetime)
    {
        var services = new ServiceCollection();
        services.AddApplicationModule();

        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);
        Assert.NotNull(descriptor);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    [Theory]
    [InlineData(typeof(IdentitySeedService), typeof(IdentitySeedService), ServiceLifetime.Scoped)]
    [InlineData(typeof(SeedDataService), typeof(SeedDataService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IUserSecurityService), typeof(UserSecurityService), ServiceLifetime.Scoped)]
    [InlineData(typeof(IStoreAccountCredentialProtector), typeof(StoreAccountCredentialProtector), ServiceLifetime.Scoped)]
    [InlineData(typeof(IReservationPolicyProvider), typeof(ReservationPolicyProvider), ServiceLifetime.Singleton)]
    [InlineData(typeof(IOperationalPolicyProvider), typeof(OperationalPolicyProvider), ServiceLifetime.Singleton)]
    [InlineData(typeof(IBankaHareketiParser), typeof(AkbankCsvParser), ServiceLifetime.Singleton)]
    [InlineData(typeof(IMailService), typeof(SmtpMailService), ServiceLifetime.Scoped)]
    [InlineData(typeof(ISecureTokenService), typeof(SecureTokenService), ServiceLifetime.Singleton)]
    public void AddInfrastructureModule_ShouldRegisterInfrastructureServicesWithCorrectLifetime(
        Type serviceType, Type implementationType, ServiceLifetime expectedLifetime)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddInfrastructureModule(configuration);

        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);
        Assert.NotNull(descriptor);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    [Theory]
    [InlineData(typeof(ICurrentUserContext), typeof(CurrentUserContext), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRazorViewToStringRenderer), typeof(RazorViewToStringRenderer), ServiceLifetime.Scoped)]
    [InlineData(typeof(IRequestContext), typeof(HttpRequestContext), ServiceLifetime.Scoped)]
    public void AddWebModule_ShouldRegisterWebServicesWithCorrectLifetime(
        Type serviceType, Type implementationType, ServiceLifetime expectedLifetime)
    {
        var services = new ServiceCollection();
        services.AddWebModule();

        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);
        Assert.NotNull(descriptor);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    [Fact]
    public void AddApplicationModule_ShouldAlsoRegisterAllFiveBusinessRules()
    {
        var services = new ServiceCollection();
        services.AddApplicationModule();

        Assert.Contains(services, d => d.ServiceType == typeof(IOnlinePaymentBusinessRules) && d.ImplementationType == typeof(OnlinePaymentBusinessRules));
        Assert.Contains(services, d => d.ServiceType == typeof(IPaymentBusinessRules) && d.ImplementationType == typeof(PaymentBusinessRules));
        Assert.Contains(services, d => d.ServiceType == typeof(IPaymentStoreRoutingBusinessRules) && d.ImplementationType == typeof(PaymentStoreRoutingBusinessRules));
        Assert.Contains(services, d => d.ServiceType == typeof(IStoreBusinessRules) && d.ImplementationType == typeof(StoreBusinessRules));
        Assert.Contains(services, d => d.ServiceType == typeof(IReservationBusinessRules) && d.ImplementationType == typeof(ReservationBusinessRules));
    }
}
