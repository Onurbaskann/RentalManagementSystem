using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PaymentLinkRecordRepository(ApplicationDbContext context)
    : BaseRepository<PaymentLinkRecord>(context), IPaymentLinkRecordRepository
{
    public Task<PaymentLinkRecord?> GetByIdIgnoringFiltersAsync(
        int recordId,
        CancellationToken cancellationToken = default)
        => _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(record => record.Id == recordId, cancellationToken);
}
