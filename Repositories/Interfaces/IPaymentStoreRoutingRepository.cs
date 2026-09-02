using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Repositories.Interfaces;

public interface IPaymentStoreRoutingRepository : IRepositoryBase<PaymentStoreRouting>
{
    Task<PagedResult<PaymentStoreRoutingListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetHistoryCountAsync();
    Task<List<MissingDefaultRoutingDto>> GetMissingDefaultsAsync();
    Task<PaymentStoreRouting?> FindActiveAsync(
        int chargeTypeId,
        int? propertyId,
        int? unitId,
        bool tracking = true);
    Task<PaymentStoreRouting?> GetTrackedByIdAsync(int id);
    Task<int?> GetDefaultStoreIdAsync(int chargeTypeId);
    Task<bool> HasUsableDefaultAsync(int chargeTypeId);
    Task<PaymentRoutingResolutionCandidateDto?> GetResolutionCandidateAsync(
        int chargeTypeId,
        int unitId,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveRoutingForStoreAsync(int storeId);
}
