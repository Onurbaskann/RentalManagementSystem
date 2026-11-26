using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Identity;
using KiraTakip.Infrastructure.Seeding;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Settings;

namespace KiraTakip.Web.Hosting
{
    public static class SeedingExtensions
    {
        private static async Task EnsureMissingSystemSettingsAsync(ApplicationDbContext dbContext)
        {
            var existingKeys = await dbContext.SystemSettings.IgnoreQueryFilters()
                .Where(setting => !setting.IsDeleted)
                .Select(setting => setting.Key)
                .ToListAsync();

            var missing = SystemSettingDefinitions.All
                .Where(definition => !existingKeys.Contains(definition.Key))
                .Select(definition => new SystemSetting { Key = definition.Key, Value = definition.DefaultValue })
                .ToList();

            if (missing.Count == 0)
                return;

            dbContext.SystemSettings.AddRange(missing);
            await dbContext.SaveChangesAsync();
        }

        public static async Task SeedAsync(this WebApplication app)
        {
            BranchDatabaseGuard.EnsureAllowed(app.Configuration.GetConnectionString("DefaultConnection"));

            using (var scope = app.Services.CreateScope())
            {
                var seedService = scope.ServiceProvider.GetRequiredService<IdentitySeedService>();
                await seedService.SeedAsync();

                var runSeed = app.Configuration.GetValue<bool>("SeedData:RunSeed");

                var domainSeed = scope.ServiceProvider.GetRequiredService<SeedDataService>();
                var spaceSeed = scope.ServiceProvider.GetRequiredService<SpaceSeedData>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (app.Environment.IsDevelopment())
                    await EnsureMissingSystemSettingsAsync(dbContext);

                // Veritabanının boş olup olmadığını kontrol et (örneğin hiç Taşınmaz/Property tanımı yoksa boş kabul edelim)
                var isDbEmpty = !await dbContext.Properties.AnyAsync();

                if (app.Environment.IsDevelopment() && (runSeed || isDbEmpty))
                {
                    // Ek güvenlik kontrolü: Veritabanı adı "KiraTakipDb" (prod db) ise kesinlikle silme/seed yapma.
                    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        try
                        {
                            var builder = new SqlConnectionStringBuilder(connectionString);
                            var databaseName = builder.InitialCatalog;
                            if (!string.Equals(databaseName, "DBTeknoPark_Test", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException($"alan-hiyerarsisi dalında seed yalnız DBTeknoPark_Test veritabanında çalışır. Yapılandırılan veritabanı: {databaseName}");
                            }
                        }
                        catch (ArgumentException)
                        {
                            throw new InvalidOperationException("Bağlantı dizesi doğrulanamadı. Güvenlik nedeniyle seed işlemi iptal edildi.");
                        }
                    }

                    if (runSeed)
                    {
                        // Sadece runSeed aktifse mevcut verileri temizle, aksi takdirde (isDbEmpty durumunda) direkt seed et.
                        await domainSeed.ClearDomainDataAsync();
                    }

                    // Sistem tanımları — her ortamda idempotent çalışır
                    await domainSeed.SeedEnumDegerleriAsync();
                    await domainSeed.SeedBorcTipleriAsync();
                    await domainSeed.SeedOdemeMagazalariAsync();
                    await domainSeed.SeedTasinmazTipleriAsync();
                    await domainSeed.SeedBirimTurleriAsync();
                    await domainSeed.SeedKiraciKategorileriAsync();
                    await domainSeed.SeedSektorlerAsync();
                    await domainSeed.SeedTarifelerAsync(); // Tarife.Yil oluşur
                    await spaceSeed.SeedAsync();
                    await domainSeed.EnsureVarsayilanReservationRateOverrideAsync();

                    await domainSeed.SeedTasinmazFiyatlarAsync();
                    await domainSeed.SeedIcRollerVeKullanicilarAsync();
                    await domainSeed.SeedDomainDataAsync();
                    await domainSeed.SeedOrnekMagazaYonlendirmeleriAsync();
                    await domainSeed.SeedTahakkuklarAsync();

                    if (runSeed)
                    {
                        // Seed işlemi sırasında SaveChangesInterceptor tarafından üretilen ara denetim loglarını temizle
                        dbContext.AuditLogs.RemoveRange(dbContext.AuditLogs.IgnoreQueryFilters());
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
