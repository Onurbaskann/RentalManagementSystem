using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRepository : IRepositoryBase<Reservation>
{
    // Listeleme (DTO)
    Task<List<ReservationListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<ReservationListItemDto>> GetPagedListAsync(
        TableQuery query,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<int> GetCancelledCountAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<List<ReservationListItemDto>> GetTenantListAsync(int tenantId, DateTime currentTime, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(int tenantId, DateTime currentTime, TableQuery query, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<ReservationListItemDto?> GetByIdAsync(int id);
    Task<Reservation?> GetForOperationAsync(int id);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(int unitId, DateTime startDate, DateTime endDate);

    Task<List<int>> GetActiveUnitIdsAsync(IReadOnlyCollection<int> unitIds, DateTime now);
    Task<bool> HasPlannedForUnitTypeAsync(int unitTypeId);
    Task<bool> ExistsForUnitAsync(int unitId);
}
