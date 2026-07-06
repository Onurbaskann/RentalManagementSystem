using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IChargeService
{
    // Listeleme — DTO döner
    Task<List<ChargeListItemDto>> GetListAsync(int? leaseId = null, IReadOnlyList<int>? propertyIds = null, IReadOnlyList<int>? unitIds = null);
    Task<PagedResult<ChargeListItemDto>> GetPagedAsync(TableQuery q, int? leaseId = null, IReadOnlyList<int>? propertyIds = null, IReadOnlyList<int>? unitIds = null);
    Task<ChargeDetailDto?> GetDetailsAsync(int id);

    // Business operations
    Task UpdateDelaysAsync();
    Task UpdatePaidAmountAsync(int chargeId);
}
