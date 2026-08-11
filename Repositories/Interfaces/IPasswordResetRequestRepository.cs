namespace KiraTakip.Repositories.Interfaces;

public interface IPasswordResetRequestRepository : IRepositoryBase<PasswordResetRequest>
{
    Task<int> CountRecentPendingAsync(string userId, DateTime cutoff, CancellationToken ct = default);
    Task<PasswordResetRequest?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default);
}
