using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Payments;

namespace KiraTakip.Repositories.Payments;

public class OnlinePaymentEventRepository(ApplicationDbContext context)
    : Repository<OnlinePaymentEvent, int>(context, onlinePaymentEvent => onlinePaymentEvent.Id),
        IOnlinePaymentEventRepository
{
    public async Task AddAsync(OnlinePaymentEvent onlinePaymentEvent, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(onlinePaymentEvent, cancellationToken);
}
