using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITenantRepository : IBaseRepository<Tenant>
{
    Task<List<KiraciListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds);
    Task<KiraciDetayDto?> GetDetayAsync(int id);
    Task<List<string>> GetExistingTenantNosAsync();
    Task<int?> GetKategoriIdAsync(int tenantId);
}
