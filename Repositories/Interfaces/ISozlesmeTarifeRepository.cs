using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ISozlesmeTarifeRepository : IBaseRepository<LeaseRateOverride>
{
    Task<RateValueDto?> GetRateAsync(int leaseId, int chargeTypeId);
}
