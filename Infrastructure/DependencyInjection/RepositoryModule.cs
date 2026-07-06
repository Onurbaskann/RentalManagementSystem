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
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<IPropertyRepository, PropertyRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<ILeaseRepository, LeaseRepository>();
            services.AddScoped<IChargeRepository, ChargeRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
            services.AddScoped<ITasinmazTarifeRepository, TasinmazTarifeRepository>();
            services.AddScoped<IChargeTypeRepository, ChargeTypeRepository>();
            services.AddScoped<ISozlesmeTarifeRepository, SozlesmeTarifeRepository>();
            services.AddScoped<IBirimTarifeRepository, BirimTarifeRepository>();
            services.AddScoped<IGenelTarifeRepository, GenelTarifeRepository>();
            services.AddScoped<IRezervasyonTarifeRepository, RezervasyonTarifeRepository>();
            services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IKategoriRepository, KategoriRepository>();
            services.AddScoped<ITasinmazTipiRepository, TasinmazTipiRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();

            return services;
        }
    }
}
