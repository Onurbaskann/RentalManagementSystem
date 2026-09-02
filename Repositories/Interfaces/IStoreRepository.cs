using KiraTakip.Models.Dtos.Store;
using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Repositories.Interfaces;

public interface IStoreRepository : IRepositoryBase<Store>
{
    Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery query);
    Task<StoreDetailDto?> GetDetailAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<StoreRoutingOptionDto>> GetRoutingOptionsAsync();
}
