using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

                if (app.Environment.IsDevelopment() && runSeed)
                {
                    await domainSeed.ClearDomainDataAsync();

                    // Sistem tanımları — her ortamda idempotent çalışır
                    await domainSeed.SeedEnumDegerleriAsync();
                    await domainSeed.SeedBorcTipleriAsync();
                    await domainSeed.SeedTasinmazTipleriAsync();
                    await domainSeed.SeedBirimTurleriAsync();
                    await domainSeed.SeedKiraciKategorileriAsync();
                    await domainSeed.SeedSektorlerAsync();
                    await domainSeed.SeedTarifelerAsync(); // Tarife.Yil oluşur
                    await domainSeed.EnsureVarsayilanRezervasyonTarifeAsync();

                    await domainSeed.SeedTasinmazFiyatlarAsync();
                    await domainSeed.SeedDomainDataAsync();
                    await domainSeed.SeedTahakkuklarAsync();
                }
            }
        }
    }
}
