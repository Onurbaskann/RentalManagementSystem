using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IPasswordResetRequestRepository : IRepositoryBase<PasswordResetRequest>
{
    Task<int> CountRecentPendingAsync(string userId, DateTime cutoff, CancellationToken ct = default);
    Task<PasswordResetRequest?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default);
}
