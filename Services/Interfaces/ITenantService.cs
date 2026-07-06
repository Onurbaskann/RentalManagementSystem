using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITenantService
{
    Task<List<KiraciListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null);
    Task<KiraciDetayDto?> GetDetayAsync(int id);
    Task<Tenant> CreateAsync(Tenant k);
    Task UpdateAsync(Tenant k);
    Task<string> GenerateKiraciNoAsync();
    Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null);
}
