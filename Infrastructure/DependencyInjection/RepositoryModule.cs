using KiraTakip.Data;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class RepositoryModule
    {
        public static IServiceCollection AddRepositoryModule(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IKiraciRepository, KiraciRepository>();
            services.AddScoped<ITasinmazRepository, TasinmazRepository>();
            services.AddScoped<IBirimRepository, BirimRepository>();
            services.AddScoped<ISozlesmeRepository, SozlesmeRepository>();
            services.AddScoped<ITahakkukRepository, TahakkukRepository>();
            services.AddScoped<IOdemeRepository, OdemeRepository>();
            services.AddScoped<IBankaHareketiRepository, BankaHareketiRepository>();
            services.AddScoped<ITasinmazTarifeRepository, TasinmazTarifeRepository>();
            services.AddScoped<IBorcTipiRepository, BorcTipiRepository>();
            services.AddScoped<ISozlesmeTarifeRepository, SozlesmeTarifeRepository>();
            services.AddScoped<IBirimTarifeRepository, BirimTarifeRepository>();
            services.AddScoped<IGenelTarifeRepository, GenelTarifeRepository>();
            services.AddScoped<IRezervasyonTarifeRepository, RezervasyonTarifeRepository>();
            services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
            services.AddScoped<IBelgeTuruRepository, BelgeTuruRepository>();
            services.AddScoped<IKategoriRepository, KategoriRepository>();
            services.AddScoped<ITasinmazTipiRepository, TasinmazTipiRepository>();
            services.AddScoped<IRezervasyonRepository, RezervasyonRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();

            return services;
        }
    }
}
