using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.AdminUser;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Domain.Leases;

namespace KiraTakip.Repositories.Interfaces.Properties;

public interface IUnitRepository : IRepositoryBase<Unit>
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
        List<int>? authorizedUnitIds = null,
        int? includedUnitId = null);
    Task<LeaseUnitContextDto?> GetLeaseContextAsync(int unitId);
    Task<List<UnitLookupDto>> GetAllOptionsAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<List<TenantChargeUnitOptionDto>> GetTenantLeaseOptionsAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task RemoveStructureDataAsync(IReadOnlyCollection<Unit> units);
    Task RemoveWithRatesAsync(Unit unit);
    void Remove(Unit unit);
    Task<bool> HasHistoricalDependencyAsync(int unitId);
    Task<List<CapacityOccupant>> GetCapacityOccupantsAsync(int unitId, DateTime fromDate);
    void AddCapacityHistory(Unit unit, int capacity, DateTime effectiveFrom);
    Task<HashSet<int>> GetOpenLeaseUnitIdsAsync(IReadOnlyCollection<int> unitIds);
}
