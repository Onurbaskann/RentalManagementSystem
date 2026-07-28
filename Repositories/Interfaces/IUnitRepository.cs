using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitRepository : IBaseRepository<Unit>
{
    Task<List<AdminUserUnitOptionDto>> GetAdminUserOptionsAsync(CancellationToken ct = default);
    Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId);
    Task<UnitDetailDto?> GetDetayAsync(int id);
    Task<List<UnitListItemDto>> GetReservableUnitsAsync(
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<ReservationUnitContextDto?> GetReservationContextAsync(int unitId);
    Task<int?> GetPropertyIdAsync(int unitId);
    Task<List<UnitLookupDto>> GetAvailableAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<LeaseUnitContextDto?> GetLeaseContextAsync(int unitId);
    Task<List<UnitLookupDto>> GetAllOptionsAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<List<TenantChargeUnitOptionDto>> GetTenantLeaseOptionsAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task RemoveStructureDataAsync(IReadOnlyCollection<Unit> units);
    Task RemoveWithRatesAsync(Unit unit);
    void Remove(Unit unit);
    Task<bool> HasHistoricalDependencyAsync(int unitId);
}
