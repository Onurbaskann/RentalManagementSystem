using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IPaymentLinkRecordRepository : IRepositoryBase<PaymentLinkRecord>
{
    Task<PaymentLinkRecord?> GetByIdIgnoringFiltersAsync(
        int recordId,
        CancellationToken cancellationToken = default);
}
