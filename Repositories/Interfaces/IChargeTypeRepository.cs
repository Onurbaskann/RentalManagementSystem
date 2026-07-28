using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IChargeTypeRepository : IBaseRepository<ChargeType>
{
    Task<List<ChargeTypeLookupDto>> GetManualChargeTypesAsync();
    Task<ChargeType?> GetActiveManualByIdAsync(int id);
    Task<List<ChargeTypeListItemDto>> GetListAsync();
    Task<int> GetMaxSortOrderAsync();
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<ChargeTypeLookupDto>> GetRezervasyonAdaylariAsync();
    Task<bool> IsActiveReservationSpecificAsync(int id);
    Task<List<ChargeType>> GetActiveGenerationTypesAsync();
    Task<List<ChargeType>> GetPricingMatrixTypesAsync();
    Task<ChargeType?> ResolveReservationTypeAsync(int? preferredChargeTypeId);
}
