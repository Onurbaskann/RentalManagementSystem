using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPaymentRepository : IBaseRepository<PaymentAllocation>
{
    Task<List<OdemeListItemDto>> GetListAsync(int? tahakkukId, List<int>? yetkiliPropertyIds);
    Task<PagedResult<OdemeListItemDto>> GetPagedListAsync(TableQuery q, int? tahakkukId, List<int>? yetkiliPropertyIds);
    Task<PaymentDetailDto?> GetDetayAsync(int id);
}
