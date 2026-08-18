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
    Task<List<ReservationListItemDto>> GetTenantListAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(int tenantId, TableQuery query, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<ReservationListItemDto?> GetByIdAsync(int id);
    Task<Reservation?> GetForOperationAsync(int id);
    Task<int?> GetUnitIdAsync(int reservationId);
    Task AcquireUnitDecisionLockAsync(int unitId);
    Task<List<int>> GetCompletionCandidateIdsAsync(DateTime cutoff, int batchSize);
    Task AcquireCompletionLockAsync(int reservationId);
    Task<Reservation?> GetForCompletionAsync(int reservationId);

    Task<List<ReservationCalendarItemDto>> GetCalendarItemsAsync(
        ReservationCalendarRepositoryQuery query);
    Task<List<TenantReservationCalendarItemDto>> GetTenantCalendarItemsAsync(
        int tenantId,
        ReservationCalendarRepositoryQuery query);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(
        int unitId,
        DateTime startDate,
        DateTime endDate,
        int? excludedReservationId = null);

    Task<List<int>> GetActiveUnitIdsAsync(IReadOnlyCollection<int> unitIds, DateTime now);
    Task<bool> HasConfirmedForUnitTypeAsync(int unitTypeId);
    Task<bool> ExistsForUnitAsync(int unitId);
}
