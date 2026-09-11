using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Payments;

/// <summary>
/// OnlinePaymentEvent append-only olduğu için (AuditLog gibi BaseEntity'den türemez)
/// yalnızca AddAsync sunulur.
/// </summary>
public interface IOnlinePaymentEventRepository : IRepository<OnlinePaymentEvent, int>
{
    Task AddAsync(OnlinePaymentEvent onlinePaymentEvent, CancellationToken cancellationToken = default);
}
