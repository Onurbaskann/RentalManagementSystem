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
            services.AddScoped<ITenantUserService, TenantUserService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<ILeaseService, LeaseService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<SeedDataService>();

            // Domain & Calculation Services
            services.AddScoped<IChargeService, ChargeService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IBankTransactionService, BankTransactionService>();
            services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();
            services.AddScoped<IRateResolverService, RateResolverService>();
            services.AddScoped<IChargeGenerationService, ChargeGenerationService>();
            services.AddScoped<IManualChargeService, ManualChargeService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IPropertyPricingService, PropertyPricingService>();
            services.AddScoped<IRateHierarchyService, RateHierarchyService>();

            // Notification, Payment & Integration Services
            services.AddScoped<IMailService, SmtpMailService>();
            services.AddScoped<IPaymentLinkService, PaymentLinkService>();
            services.AddScoped<IChargeReminderService, ChargeReminderService>();
            services.AddSingleton<ISecureTokenService, SecureTokenService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<IDocumentService, DocumentService>();

            return services;
        }
    }
}
