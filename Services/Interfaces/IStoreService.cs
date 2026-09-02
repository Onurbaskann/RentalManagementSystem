using KiraTakip.Models.Dtos.Store;

namespace KiraTakip.Services.Interfaces;

public interface IStoreService
{
    Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery query);
    Task<StoreDetailDto?> GetDetailAsync(int id);
    Task<int> CreateAsync(CreateStoreInput input);
    Task UpdateAsync(int id, UpdateStoreInput input);
    Task<bool> ToggleStatusAsync(int id);
    Task ReplaceAccountAsync(CreateStoreAccountVersionInput input);
    Task DeactivateAccountAsync(int storeId, int accountId);
}
