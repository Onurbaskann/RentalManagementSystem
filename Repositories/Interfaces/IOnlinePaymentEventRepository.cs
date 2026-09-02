using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

/// <summary>
/// OnlinePaymentEvent append-only olduğu için (AuditLog gibi BaseEntity'den türemez)
/// yalnızca AddAsync sunulur.
/// </summary>
public interface IOnlinePaymentEventRepository : IRepository<OnlinePaymentEvent, int>
{
    Task AddAsync(OnlinePaymentEvent onlinePaymentEvent, CancellationToken cancellationToken = default);
}
