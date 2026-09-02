using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Repositories;

public class OnlinePaymentEventRepository(ApplicationDbContext context)
    : Repository<OnlinePaymentEvent, int>(context, onlinePaymentEvent => onlinePaymentEvent.Id),
        IOnlinePaymentEventRepository
{
    public async Task AddAsync(OnlinePaymentEvent onlinePaymentEvent, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(onlinePaymentEvent, cancellationToken);
}
