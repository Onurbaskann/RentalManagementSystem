using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Models.Dtos.Store;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IStoreRepository : IRepositoryBase<Store>
{
    Task<PagedResult<StoreListItemDto>> GetPagedListAsync(TableQuery query);
    Task<StoreDetailDto?> GetDetailAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<StoreRoutingOptionDto>> GetRoutingOptionsAsync();
    Task<Store?> GetWithAccountsByIdAsync(int id, CancellationToken cancellationToken = default);
}
