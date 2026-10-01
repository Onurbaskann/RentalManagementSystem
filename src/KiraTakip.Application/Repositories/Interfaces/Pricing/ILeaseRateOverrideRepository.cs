using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface ILeaseRateOverrideRepository : IRepositoryBase<LeaseRateOverride>
{
    Task<RateValueDto?> GetRateAsync(int leaseId, int chargeTypeId);
    // Toplu (bulk) hali — GetRateAsync ile birebir aynı filtre (IsActive kontrolü yok), yalnız IN(...) ile genişletilmiş.
    Task<List<(int LeaseId, int ChargeTypeId, RateValueDto Rate)>> GetRatesAsync(
        IReadOnlyCollection<int> leaseIds, IReadOnlyCollection<int> chargeTypeIds);
    Task ReplaceAsync(int leaseId, IReadOnlyCollection<LeaseRateOverride> rateOverrides);
    Task<List<LeaseRateOverride>> GetWithChargeTypeAsync(int leaseId);
    Task SoftDeleteByLeaseIdAsync(int leaseId);
}
