using KiraTakip.Data;
using KiraTakip.Infrastructure.Auditing;
using KiraTakip.Infrastructure.Notifications;
using KiraTakip.Infrastructure.Payments;
using KiraTakip.Infrastructure.Persistence;
using KiraTakip.Infrastructure.Seeding;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Banking;
using KiraTakip.Services.Identity;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Notifications;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Security;
using KiraTakip.Services.Notifications;
using KiraTakip.Services.Payments;
using KiraTakip.Services.Reservations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructureModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Database, Interceptors & Context
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddScoped<PermissionCacheTransactionInterceptor>();
            services.AddSingleton<IUniqueConstraintViolationDetector, SqlServerUniqueConstraintViolationDetector>();
            services.AddSingleton<IConcurrencyViolationDetector, EfCoreConcurrencyViolationDetector>();
            services.AddSingleton<ISmtpConfigurationValidator, SmtpConfigurationValidator>();
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(
                    sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                    sp.GetRequiredService<PermissionCacheTransactionInterceptor>());
            });

            // Memory Cache & Security Scope Providers
            services.AddMemoryCache();
            services.AddSingleton<IPermissionScopeCache, PermissionScopeCacheService>();
            services.AddSingleton<IUserPermissionCache, UserPermissionCacheService>();
            services.AddScoped<IPermissionCacheTransactionState, PermissionCacheTransactionState>();
            services.AddScoped<IUserPermissionCacheInvalidator, UserPermissionCacheInvalidator>();



            // Infrastructure Services
            services.AddScoped<IdentitySeedService>();
            services.AddScoped<SeedDataService>();
            services.AddScoped<IUserSecurityService, UserSecurityService>();
            services.AddScoped<IStoreAccountCredentialProtector, StoreAccountCredentialProtector>();
            services.AddSingleton<IReservationPolicyProvider, ReservationPolicyProvider>();
            services.AddSingleton<IOperationalPolicyProvider, OperationalPolicyProvider>();
            services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();
            services.AddScoped<IMailService, SmtpMailService>();
            services.AddSingleton<ISecureTokenService, SecureTokenService>();

            // Options Pattern Settings Configuration
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
            services.Configure<SecureTokenSettings>(configuration.GetSection("SecureToken"));
            services.Configure<ReservationCompletionSettings>(configuration.GetSection("ReservationCompletion"));
            services.Configure<DataProtectionSettings>(configuration.GetSection("DataProtection"));
            services.Configure<ParatikaOptions>(configuration.GetSection("Paratika"));
            services.AddHttpClient<IOnlinePaymentProvider, ParatikaOnlinePaymentProvider>((sp, client) =>
            {
                var paratikaOptions = sp.GetRequiredService<IOptions<ParatikaOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(paratikaOptions.HttpTimeoutSeconds);
            });
            var dataProtection = services.AddDataProtection().SetApplicationName("KiraTakip");
            var keyRingPath = configuration["DataProtection:KeyRingPath"];
            if (!string.IsNullOrWhiteSpace(keyRingPath))
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keyRingPath)));
            services.AddSingleton(TimeProvider.System);

            // Hashids Configuration
            var hashidsSection = configuration.GetSection("Hashids");
            services.Configure<Hashids.HashidsSettings>(hashidsSection);
            var hashidsSettings = hashidsSection.Get<Hashids.HashidsSettings>() ?? new Hashids.HashidsSettings();
            services.AddSingleton<HashidsNet.IHashids>(new HashidsNet.Hashids(hashidsSettings.Salt, hashidsSettings.MinHashLength));
            services.AddSingleton<KiraTakip.Common.IHashIdEncoder, KiraTakip.Infrastructure.Hashids.HashIdEncoder>();

            return services;
        }
    }
}
