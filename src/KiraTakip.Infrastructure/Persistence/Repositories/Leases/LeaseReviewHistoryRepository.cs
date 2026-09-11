using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Leases;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Repositories.Leases;

public class LeaseReviewHistoryRepository(ApplicationDbContext context)
    : RepositoryBase<LeaseReviewHistory>(context), ILeaseReviewHistoryRepository
{
    public Task<List<LeaseReviewHistoryDto>> GetByLeaseIdAsync(int leaseId)
        => Project(_dbSet
                .AsNoTracking()
                .Where(history => history.LeaseId == leaseId)
                .OrderBy(history => history.ActionDate)
                .ThenBy(history => history.Id))
            .ToListAsync();

    public Task<LeaseReviewHistoryDto?> GetLatestRevisionAsync(int leaseId)
        => Project(_dbSet
                .AsNoTracking()
                .Where(history => history.LeaseId == leaseId
                    && history.ActionType == LeaseReviewActionType.RevisionRequested)
                .OrderByDescending(history => history.ActionDate)
                .ThenByDescending(history => history.Id))
            .FirstOrDefaultAsync();

    private static IQueryable<LeaseReviewHistoryDto> Project(
        IQueryable<LeaseReviewHistory> query)
        => query.Select(history => new LeaseReviewHistoryDto(
            history.Id,
            history.ActionType,
            history.FromStatus,
            history.ToStatus,
            history.Explanation,
            history.ActorUser.AdSoyad
                ?? history.ActorUser.UserName
                ?? history.ActorUser.Email
                ?? history.ActorUserId,
            history.ActionDate));
}
