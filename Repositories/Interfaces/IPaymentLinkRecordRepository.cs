using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IPaymentLinkRecordRepository : IBaseRepository<PaymentLinkRecord>
{
    Task<PaymentLinkRecord?> GetByIdIgnoringFiltersAsync(
        int recordId,
        CancellationToken cancellationToken = default);
}
