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
            services.AddScoped<IUserRolService, UserRolService>();
            services.AddScoped<IUserSecurityService, UserSecurityService>();
            services.AddScoped<IRolService, RolService>();
            services.AddScoped<IKiraciKullaniciService, KiraciKullaniciService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ITasinmazService, TasinmazService>();
            services.AddScoped<IBirimService, BirimService>();
            services.AddScoped<IKiraciService, KiraciService>();
            services.AddScoped<ISozlesmeService, SozlesmeService>();
            services.AddScoped<IIstatistikService, IstatistikService>();
            services.AddScoped<SeedDataService>();

            // Domain & Calculation Services
            services.AddScoped<ITahakkukService, TahakkukService>();
            services.AddScoped<IOdemeService, OdemeService>();
            services.AddScoped<IBankaHareketiService, BankaHareketiService>();
            services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();
            services.AddScoped<IRateResolverService, RateResolverService>();
            services.AddScoped<ITahakkukUretimService, TahakkukUretimService>();
            services.AddScoped<IManuelBorcService, ManuelBorcService>();
            services.AddScoped<IRezervasyonService, RezervasyonService>();
            services.AddScoped<ITasinmazFiyatService, TasinmazFiyatService>();
            services.AddScoped<ITarifeHiyerarsiService, TarifeHiyerarsiService>();

            // Notification, Payment & Integration Services
            services.AddScoped<IMailService, SmtpMailService>();
            services.AddScoped<IPaymentLinkService, PaymentLinkService>();
            services.AddScoped<IBorcHatirlatmaService, BorcHatirlatmaService>();
            services.AddSingleton<ISecureTokenService, SecureTokenService>();
            services.AddScoped<IDavetiyeService, DavetiyeService>();
            services.AddScoped<ISifreSifirlamaService, SifreSifirlamaService>();
            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<IBelgeService, BelgeService>();

            return services;
        }
    }
}
