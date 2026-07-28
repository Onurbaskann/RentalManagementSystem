using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class PasswordResetRequestRepository(ApplicationDbContext context)
    : BaseRepository<PasswordResetRequest>(context), IPasswordResetRequestRepository
{
    public Task<int> CountRecentPendingAsync(string userId, DateTime cutoff, CancellationToken ct = default)
        => _dbSet.CountAsync(request => request.UserId == userId
            && request.Status == PasswordResetStatus.Pending
            && request.CreatedAt >= cutoff, ct);

    public Task<PasswordResetRequest?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(request => request.TokenHash == tokenHash, ct);
}
