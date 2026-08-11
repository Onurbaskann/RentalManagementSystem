using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ILeaseRateOverrideRepository : IRepositoryBase<LeaseRateOverride>
{
    Task<RateValueDto?> GetRateAsync(int leaseId, int chargeTypeId);
    Task ReplaceAsync(int leaseId, IReadOnlyCollection<LeaseRateOverride> rateOverrides);
    Task<List<LeaseRateOverride>> GetWithChargeTypeAsync(int leaseId);
    Task SoftDeleteByLeaseIdAsync(int leaseId);
}
