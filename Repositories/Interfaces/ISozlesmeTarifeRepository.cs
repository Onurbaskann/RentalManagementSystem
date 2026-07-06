using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ISozlesmeTarifeRepository : IBaseRepository<SozlesmeTarife>
{
    Task<RateValueDto?> GetRateAsync(int sozlesmeId, int chargeTypeId);
}
