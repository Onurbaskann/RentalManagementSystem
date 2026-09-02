using KiraTakip.Models.Dtos.Store;

namespace KiraTakip.Repositories.Interfaces;

public interface IStoreAccountRepository : IRepositoryBase<StoreAccount>
{
    Task<StoreAccount?> GetActiveByStoreIdAsync(int storeId, bool tracking = true);
    Task<List<StoreAccountHistoryItemDto>> GetHistoryByStoreIdAsync(int storeId);
}
