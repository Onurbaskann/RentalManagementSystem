using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface ILeaseRateOverrideRepository : IRepositoryBase<LeaseRateOverride>
{
    Task<RateValueDto?> GetRateAsync(int leaseId, int chargeTypeId);
    Task ReplaceAsync(int leaseId, IReadOnlyCollection<LeaseRateOverride> rateOverrides);
    Task<List<LeaseRateOverride>> GetWithChargeTypeAsync(int leaseId);
    Task SoftDeleteByLeaseIdAsync(int leaseId);
}
