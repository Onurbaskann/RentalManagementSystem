using KiraTakip.Models.Dtos.Store;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IStoreAccountRepository : IRepositoryBase<StoreAccount>
{
    Task<StoreAccount?> GetActiveByStoreIdAsync(int storeId, bool tracking = true);
    Task<List<StoreAccountHistoryItemDto>> GetHistoryByStoreIdAsync(int storeId);
}
