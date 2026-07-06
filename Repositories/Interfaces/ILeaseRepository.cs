using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ILeaseRepository : IBaseRepository<Lease>
{
    Task<List<SozlesmeListItemDto>> GetListAsync(string? filtre, List<int>? yetkiliPropertyIds);
    Task<SozlesmeDetayDto?> GetDetayAsync(int id);
    Task<List<SozlesmeListItemDto>> GetByTenantIdAsync(int tenantId);
    Task<List<SozlesmeListItemDto>> GetByUnitIdAsync(int unitId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds);

    // Dropdown — entity döner (Tenant + Unit + Property yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<SozlesmeDropdownDto>> GetAktifDropdownAsync();

    // RateResolver için projeksiyon: TasinmazId + KiraciKategoriId
    Task<(int TasinmazId, int? KategoriId)?> GetPropertyAndCategoryAsync(int leaseId);
}
