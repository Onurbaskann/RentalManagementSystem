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
using KiraTakip.Services;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class SeedingExtensions
    {
        public static async Task SeedAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var seedService = scope.ServiceProvider.GetRequiredService<IdentitySeedService>();
                await seedService.SeedAsync();

                var runSeed = app.Configuration.GetValue<bool>("SeedData:RunSeed");

                var domainSeed = scope.ServiceProvider.GetRequiredService<SeedDataService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
                            if (string.Equals(databaseName, "KiraTakipDb", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException("Kritik Güvenlik Uyarısı: Üretim (Production) veritabanı ('KiraTakipDb') üzerinde veri temizleme ve seed işlemi gerçekleştirilemez!");
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
                    await domainSeed.SeedTasinmazTipleriAsync();
                    await domainSeed.SeedBirimTurleriAsync();
                    await domainSeed.SeedKiraciKategorileriAsync();
                    await domainSeed.SeedSektorlerAsync();
                    await domainSeed.SeedTarifelerAsync(); // Tarife.Yil oluşur
                    await domainSeed.EnsureVarsayilanReservationRateOverrideAsync();

                    await domainSeed.SeedTasinmazFiyatlarAsync();
                    await domainSeed.SeedDomainDataAsync();
                    await domainSeed.SeedTahakkuklarAsync();
                }
            }
        }
    }
}
