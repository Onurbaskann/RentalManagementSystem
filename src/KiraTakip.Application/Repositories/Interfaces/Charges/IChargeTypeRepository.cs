using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Charges;

public interface IChargeTypeRepository : IRepositoryBase<ChargeType>
{
    Task<List<ChargeTypeLookupDto>> GetManualChargeTypesAsync();
    Task<ChargeType?> GetActiveManualByIdAsync(int id);
    Task<List<ChargeTypeListItemDto>> GetListAsync();
    Task<PagedResult<ChargeTypeListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSortOrderAsync();
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<ChargeTypeLookupDto>> GetRezervasyonAdaylariAsync();
    Task<bool> IsActiveReservationSpecificAsync(int id);
    Task<bool> IsChargeTypeUsedAsync(int chargeTypeId, CancellationToken ct = default);
    Task<List<ChargeType>> GetActiveGenerationTypesAsync();
    Task<List<ChargeType>> GetPricingMatrixTypesAsync();
}
