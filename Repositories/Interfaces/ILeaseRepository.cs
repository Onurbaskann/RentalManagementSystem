using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ILeaseRepository : IBaseRepository<Lease>
{
    Task<List<LeaseListItemDto>> GetListAsync(string? filter, List<int>? authorizedPropertyIds);
    Task<LeaseDetailDto?> GetDetayAsync(int id);
    Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId);
    Task<List<LeaseListItemDto>> GetByUnitIdAsync(int unitId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds);

    // Dropdown — entity döner (Tenant + Unit + Property yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<LeaseDropdownDto>> GetAktifDropdownAsync();

    // RateResolver için projeksiyon: TasinmazId + KiraciKategoriId
    Task<(int TasinmazId, int? KategoriId)?> GetPropertyAndCategoryAsync(int leaseId);
}
