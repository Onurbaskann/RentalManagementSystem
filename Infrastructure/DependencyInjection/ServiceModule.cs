using KiraTakip.Services;
using KiraTakip.Services.Banka;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class ServiceModule
    {
        public static IServiceCollection AddServiceModule(this IServiceCollection services)
        {
            // Core Services
            services.AddScoped<IMaskingService, MaskingService>();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<IdentitySeedService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            services.AddScoped<IUserSecurityService, UserSecurityService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<ITenantUserService, TenantUserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<ILeaseService, LeaseService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<SeedDataService>();
            services.AddScoped<ISystemSettingService, SystemSettingService>();
            services.AddSingleton<IReservationPolicyProvider, ReservationPolicyProvider>();
            services.AddSingleton<IOperationalPolicyProvider, OperationalPolicyProvider>();

            // Domain & Calculation Services
            services.AddScoped<IChargeService, ChargeService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IBankTransactionService, BankTransactionService>();
            services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();
            services.AddScoped<IRateResolverService, RateResolverService>();
            services.AddScoped<IChargeGenerationService, ChargeGenerationService>();
            services.AddScoped<IManualChargeService, ManualChargeService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IReservationCompletionService, ReservationCompletionService>();
            services.AddScoped<IPropertyPricingService, PropertyPricingService>();
            services.AddScoped<IUnitPricingService, UnitPricingService>();
            services.AddScoped<IRateHierarchyService, RateHierarchyService>();
            services.AddScoped<IChargeTypeService, ChargeTypeService>();
            services.AddScoped<IUnitTypeService, UnitTypeService>();

            // Notification, Payment & Integration Services
            services.AddScoped<IMailService, SmtpMailService>();
            services.AddScoped<IPaymentLinkService, PaymentLinkService>();
            services.AddScoped<IPaymentPortalService, PaymentPortalService>();
            services.AddScoped<IChargeReminderService, ChargeReminderService>();
            services.AddSingleton<ISecureTokenService, SecureTokenService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IPropertyTypeService, PropertyTypeService>();
            services.AddScoped<IRateScheduleService, RateScheduleService>();
            services.AddScoped<ISectorService, SectorService>();
            services.AddScoped<ITenantCategoryService, TenantCategoryService>();
            services.AddScoped<ITenantPanelService, TenantPanelService>();

            return services;
        }
    }
}
