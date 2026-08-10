using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class LeaseReviewHistoryRepository(ApplicationDbContext context)
    : ILeaseReviewHistoryRepository
{
    public Task AddAsync(LeaseReviewHistory history)
        => context.SozlesmeIncelemeGecmisleri.AddAsync(history).AsTask();

    public Task<List<LeaseReviewHistoryDto>> GetByLeaseIdAsync(int leaseId)
        => Project(context.SozlesmeIncelemeGecmisleri
                .AsNoTracking()
                .Where(history => history.LeaseId == leaseId)
                .OrderBy(history => history.ActionDate)
                .ThenBy(history => history.Id))
            .ToListAsync();

    public Task<LeaseReviewHistoryDto?> GetLatestRevisionAsync(int leaseId)
        => Project(context.SozlesmeIncelemeGecmisleri
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
