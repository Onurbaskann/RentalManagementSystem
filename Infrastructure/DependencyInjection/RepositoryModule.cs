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
            services.AddScoped<ILeaseReviewHistoryRepository, LeaseReviewHistoryRepository>();
            services.AddScoped<IChargeRepository, ChargeRepository>();
            services.AddScoped<IChargeLineItemRepository, ChargeLineItemRepository>();
            services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();
            services.AddScoped<IPaymentMatchRepository, PaymentMatchRepository>();
            services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
            services.AddScoped<IPropertyRateOverrideRepository, PropertyRateOverrideRepository>();
            services.AddScoped<IChargeTypeRepository, ChargeTypeRepository>();
            services.AddScoped<ILeaseRateOverrideRepository, LeaseRateOverrideRepository>();
            services.AddScoped<IUnitRateRepository, UnitRateRepository>();
            services.AddScoped<IRateScheduleRepository, RateScheduleRepository>();
            services.AddScoped<IReservationRateOverrideRepository, ReservationRateOverrideRepository>();
            services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDocumentContentRepository, DocumentContentRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPropertyTypeRepository, PropertyTypeRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IReservationAttendeeRepository, ReservationAttendeeRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
            services.AddScoped<IUserPermissionScopeRepository, UserPermissionScopeRepository>();
            services.AddScoped<IInvitationRepository, InvitationRepository>();
            services.AddScoped<IPasswordResetRequestRepository, PasswordResetRequestRepository>();
            services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IStoreAccountRepository, StoreAccountRepository>();
            services.AddScoped<IPaymentStoreRoutingRepository, PaymentStoreRoutingRepository>();
            services.AddScoped<IOnlinePaymentTransactionRepository, OnlinePaymentTransactionRepository>();
            services.AddScoped<IOnlinePaymentEventRepository, OnlinePaymentEventRepository>();

            return services;
        }
    }
}
