using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentService
{
    Task<List<OdemeListItemDto>> GetAllAsync(int? tahakkukId = null, IReadOnlyList<int>? tasinmazIds = null);
    Task<PagedResult<OdemeListItemDto>> GetPagedAsync(TableQuery q, int? tahakkukId = null, IReadOnlyList<int>? tasinmazIds = null);
    Task<OdemeDetayDto?> GetByIdAsync(int id);
    Task<PaymentAllocation> EkleAsync(PaymentAllocation payment);
    Task<bool> OnaylaAsync(int id, string onaylayanUserId);
    Task<bool> ReddetAsync(int id, string neden);
}
