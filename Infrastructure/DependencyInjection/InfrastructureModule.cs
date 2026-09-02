using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure;
using KiraTakip.Models.Settings;
using KiraTakip.Services;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructureModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Database, Interceptors & Context
            services.AddScoped<AuditSaveChangesInterceptor>();
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });

            // Memory Cache & Security Scope Providers
            services.AddMemoryCache();
            services.AddSingleton<IPermissionScopeCache, PermissionScopeCacheService>();
            services.AddScoped<IPermissionScopeProvider, PermissionScopeProvider>();
            services.AddScoped<YetkiKapsamiActionFilter>();

            // Options Pattern Settings Configuration
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
            services.Configure<SecureTokenSettings>(configuration.GetSection("SecureToken"));
            services.Configure<ReservationCompletionSettings>(configuration.GetSection("ReservationCompletion"));
            services.Configure<DataProtectionSettings>(configuration.GetSection("DataProtection"));
            services.Configure<ParatikaOptions>(configuration.GetSection("Paratika"));
            var dataProtection = services.AddDataProtection().SetApplicationName("KiraTakip");
            var keyRingPath = configuration["DataProtection:KeyRingPath"];
            if (!string.IsNullOrWhiteSpace(keyRingPath))
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keyRingPath)));
            services.AddSingleton(TimeProvider.System);
            services.AddHostedService<ReservationCompletionBackgroundService>();

            // Hashids Configuration
            var hashidsSection = configuration.GetSection("Hashids");
            services.Configure<Hashids.HashidsSettings>(hashidsSection);
            var hashidsSettings = hashidsSection.Get<Hashids.HashidsSettings>() ?? new Hashids.HashidsSettings();
            services.AddSingleton<HashidsNet.IHashids>(new HashidsNet.Hashids(hashidsSettings.Salt, hashidsSettings.MinHashLength));

            return services;
        }
    }
}
