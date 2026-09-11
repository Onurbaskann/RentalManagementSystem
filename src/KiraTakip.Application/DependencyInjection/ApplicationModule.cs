using System.Linq;
using System.Reflection;
using KiraTakip.Infrastructure.Exceptions;
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
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Reporting;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Security;
using KiraTakip.Services.Interfaces.Settings;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Payments;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Properties;
using KiraTakip.Services.Reporting;
using KiraTakip.Services.Reservations;
using KiraTakip.Services.Security;
using KiraTakip.Services.Settings;
using KiraTakip.Services.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Application.DependencyInjection;

public static class ApplicationModule
{
    public static IServiceCollection AddApplicationModule(this IServiceCollection services)
    {
        // Security & Identity
        services.AddScoped<IMaskingService, MaskingService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPermissionScopeProvider, PermissionScopeProvider>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<ITenantUserService, TenantUserService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Audit
        services.AddScoped<IAuditService, AuditService>();

        // Properties & Units
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IPropertyTypeService, PropertyTypeService>();
        services.AddScoped<IUnitTypeService, UnitTypeService>();

        // Tenants
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISectorService, SectorService>();
        services.AddScoped<ITenantCategoryService, TenantCategoryService>();
        services.AddScoped<ITenantPanelService, TenantPanelService>();

        // Leases
        services.AddScoped<ILeaseService, LeaseService>();

        // Reservations
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IReservationCompletionService, ReservationCompletionService>();

        // Charges
        services.AddScoped<IChargeService, ChargeService>();
        services.AddScoped<IChargeGenerationService, ChargeGenerationService>();
        services.AddScoped<IManualChargeService, ManualChargeService>();
        services.AddScoped<IChargeTypeService, ChargeTypeService>();
        services.AddScoped<IChargeReminderService, ChargeReminderService>();

        // Pricing
        services.AddScoped<IRateResolverService, RateResolverService>();
        services.AddScoped<IPropertyPricingService, PropertyPricingService>();
        services.AddScoped<IUnitPricingService, UnitPricingService>();
        services.AddScoped<IRateHierarchyService, RateHierarchyService>();
        services.AddScoped<IRateScheduleService, RateScheduleService>();

        // Payments
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IPaymentStoreRoutingService, PaymentStoreRoutingService>();
        services.AddScoped<IPaymentStoreResolver, PaymentStoreResolver>();
        services.AddScoped<IOnlinePaymentService, OnlinePaymentService>();

        // Banking
        services.AddScoped<IBankTransactionService, BankTransactionService>();

        // Documents
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();

        // Reporting & Settings
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();

        // Domain Business Rules
        services.AddBusinessRulesModule();

        return services;
    }

    public static IServiceCollection AddBusinessRulesModule(this IServiceCollection services)
    {
        var assembly = typeof(IBusinessRules).Assembly;
        var types = assembly.GetTypes();

        var businessRuleInterfaces = types.Where(t =>
            t.IsInterface && t != typeof(IBusinessRules) && typeof(IBusinessRules).IsAssignableFrom(t));

        foreach (var iface in businessRuleInterfaces)
        {
            var implementation = types.Single(t =>
                t is { IsClass: true, IsAbstract: false } && iface.IsAssignableFrom(t));

            services.AddScoped(iface, implementation);
        }

        return services;
    }
}
