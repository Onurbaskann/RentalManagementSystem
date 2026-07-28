using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Repositories;

public class InvitationRepository(ApplicationDbContext ctx) : BaseRepository<Invitation>(ctx), IInvitationRepository
{
    public Task<List<TenantInvitationListItemDto>> GetPendingTenantListAsync(
        int tenantId,
        DateTime now,
        CancellationToken ct = default)
        => _dbSet.AsNoTracking()
            .Where(invitation => invitation.TenantId == tenantId
                && invitation.Status == InvitationStatus.Pending
                && invitation.ExpiresAt > now)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new TenantInvitationListItemDto(
                invitation.Id,
                invitation.Email,
                invitation.FullName ?? string.Empty,
                invitation.Role != null ? invitation.Role.Name : "—",
                invitation.CreatedAt,
                invitation.ExpiresAt))
            .ToListAsync(ct);

    public Task<bool> HasPendingForTenantEmailAsync(
        int tenantId,
        string email,
        DateTime now,
        CancellationToken ct = default)
        => _dbSet.AsNoTracking().AnyAsync(invitation =>
            invitation.TenantId == tenantId
            && invitation.Email == email
            && invitation.Status == InvitationStatus.Pending
            && invitation.ExpiresAt > now,
            ct);
    public Task<Invitation?> GetByIdAndTenantIdAsync(
        int id,
        int tenantId,
        CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(
            invitation => invitation.Id == id && invitation.TenantId == tenantId,
            ct);
    public Task<Invitation?> GetInternalByIdAsync(int id, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(invitation => invitation.Id == id && invitation.TenantId == null, ct);

    public Task<Invitation?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(invitation => invitation.TokenHash == tokenHash, ct);

    public Task<List<Invitation>> GetPendingInternalAsync(CancellationToken ct = default)
        => _dbSet
            .Where(invitation => invitation.Status == InvitationStatus.Pending && invitation.TenantId == null)
            .Include(invitation => invitation.Role)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .ToListAsync(ct);

    public Task MarkExpiredAsync(DateTime now, CancellationToken ct = default)
        => _dbSet
            .Where(invitation => invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt < now)
            .ExecuteUpdateAsync(update => update.SetProperty(invitation => invitation.Status, InvitationStatus.Expired), ct);
}
