using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Repositories.Interfaces.Reservations;

public interface IReservationRepository : IRepositoryBase<Reservation>
{
    // Listeleme (DTO)
    Task<List<ReservationListItemDto>> GetListAsync(
        SpaceAccessScope scope);
    Task<PagedResult<ReservationListItemDto>> GetPagedListAsync(
        TableQuery query,
        SpaceAccessScope scope);
    Task<int> GetCancelledCountAsync(
        SpaceAccessScope scope);
    Task<List<ReservationListItemDto>> GetTenantListAsync(int tenantId, SpaceAccessScope scope);
    Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(int tenantId, TableQuery query, SpaceAccessScope scope);
    Task<ReservationListItemDto?> GetByIdAsync(int id);
    Task<Reservation?> GetForOperationAsync(int id);
    Task<int?> GetSpaceIdAsync(int reservationId);
    Task AcquireSpaceDecisionLockAsync(int spaceId);
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
        int spaceId,
        DateTime startDate,
        DateTime endDate,
        int? excludedReservationId = null);

    Task<ReservationSpaceContextDto?> GetSpaceContextAsync(int spaceId);
    Task<List<ReservationSpaceOptionDto>> GetReservableSpacesAsync(SpaceAccessScope scope);
    Task<List<ReservationSpaceOptionDto>> GetActiveSpaceOptionsAsync();
    Task<List<string>> GetExistingReservationNosAsync();
}
