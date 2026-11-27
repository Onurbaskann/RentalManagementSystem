using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Leases;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Leases;

public class LeaseGroupRepository(ApplicationDbContext ctx)
    : RepositoryBase<LeaseGroup>(ctx), ILeaseGroupRepository
{
    public Task<LeaseGroup?> GetForMutationAsync(int groupId, SpaceAccessScope? scope)
        => ApplyScope(
                _dbSet
                    .Include(group => group.Space)
                    .Include(group => group.Leases).ThenInclude(lease => lease.Areas)
                    .Where(group => group.Id == groupId),
                scope)
            .FirstOrDefaultAsync();

    public async Task<LeaseGroupDetailDto?> GetDetailAsync(int groupId, SpaceAccessScope? scope)
    {
        var group = await ApplyScope(
                _dbSet.AsNoTracking().Where(item => item.Id == groupId),
                scope)
            .Select(item => new
            {
                item.Id,
                item.PreviousGroupId,
                item.SpaceId,
                SpaceName = item.Space.Name,
                SpaceArea = item.Space.Area,
                item.CreatedBy,
                item.CreatedAt
            })
            .FirstOrDefaultAsync();
        if (group == null) return null;

        var members = await ctx.Leases
            .AsNoTracking()
            .Where(lease => lease.LeaseGroupId == groupId)
            .OrderBy(lease => lease.Id)
            .Select(lease => new LeaseGroupMemberDto
            {
                LeaseId = lease.Id,
                LeaseNo = lease.LeaseNo,
                TenantId = lease.TenantId,
                TenantName = lease.Tenant.Name,
                Status = lease.Status,
                Area = lease.Areas.OrderBy(area => area.Id).Select(area => area.RentedArea).FirstOrDefault(),
                RowVersion = lease.RowVersion,
                RateOverrides = lease.LeaseRateOverrides
                    .OrderBy(rate => rate.ChargeType.Name)
                    .Select(rate => new LeaseRateDto
                    {
                        Id = rate.Id,
                        ChargeTypeId = rate.ChargeTypeId,
                        ChargeTypeCode = rate.ChargeType.Code,
                        ChargeTypeName = rate.ChargeType.Name,
                        ChargeTypeBehavior = rate.ChargeType.Behavior,
                        UnitValue = rate.UnitValue,
                        CalculationMethod = rate.CalculationMethod,
                        VatRate = rate.KdvRate
                    })
                    .ToList()
            })
            .ToListAsync();

        var first = await ctx.Leases
            .AsNoTracking()
            .Where(lease => lease.LeaseGroupId == groupId)
            .OrderBy(lease => lease.Id)
            .Select(lease => new
            {
                lease.StartDate,
                lease.EndDate,
                lease.DueDateRuleType,
                lease.DueDay,
                lease.Description,
                lease.IsRentFree,
                SnapshotPath = lease.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotPath).FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        var memberIds = members.Select(member => member.LeaseId).ToList();
        var revisions = await ctx.SozlesmeIncelemeGecmisleri
            .AsNoTracking()
            .Where(history => memberIds.Contains(history.LeaseId)
                && history.ActionType == LeaseReviewActionType.RevisionRequested)
            .OrderByDescending(history => history.ActionDate)
            .ThenByDescending(history => history.Id)
            .Select(history => new
            {
                history.LeaseId,
                Dto = new LeaseReviewHistoryDto(
                    history.Id,
                    history.ActionType,
                    history.FromStatus,
                    history.ToStatus,
                    history.Explanation,
                    history.ActorUser.AdSoyad
                        ?? history.ActorUser.UserName
                        ?? history.ActorUser.Email
                        ?? history.ActorUserId,
                    history.ActionDate)
            })
            .ToListAsync();
        foreach (var member in members)
            member.LatestRevision = revisions.FirstOrDefault(r => r.LeaseId == member.LeaseId)?.Dto;

        var ownerName = await ctx.Users
            .AsNoTracking()
            .Where(user => user.Id == group.CreatedBy)
            .Select(user => user.AdSoyad ?? user.UserName ?? user.Email ?? user.Id)
            .FirstOrDefaultAsync() ?? group.CreatedBy;

        return new LeaseGroupDetailDto
        {
            GroupId = group.Id,
            PreviousGroupId = group.PreviousGroupId,
            SpaceId = group.SpaceId,
            SpaceName = group.SpaceName,
            SpacePath = string.IsNullOrEmpty(first?.SnapshotPath) ? group.SpaceName : first.SnapshotPath,
            SpaceArea = group.SpaceArea,
            StartDate = first?.StartDate ?? default,
            EndDate = first?.EndDate ?? default,
            DueDateRuleType = first?.DueDateRuleType ?? default,
            DueDay = first?.DueDay ?? 1,
            Description = first?.Description,
            IsRentFree = first?.IsRentFree ?? false,
            OwnerUserId = group.CreatedBy,
            OwnerDisplayName = ownerName,
            CreatedAt = group.CreatedAt,
            Members = members
        };
    }

    public async Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId)
    {
        var groupId = await ctx.Leases
            .AsNoTracking()
            .Where(lease => lease.Id == leaseId)
            .Select(lease => lease.LeaseGroupId)
            .FirstOrDefaultAsync();
        if (groupId == null) return null;

        var count = await ctx.Leases.AsNoTracking()
            .CountAsync(lease => lease.LeaseGroupId == groupId);
        return new LeaseGroupLinkDto(groupId.Value, count);
    }

    public Task<List<int>> GetLeaseIdsAsync(int groupId)
        => ctx.Leases.AsNoTracking()
            .Where(lease => lease.LeaseGroupId == groupId)
            .Select(lease => lease.Id)
            .ToListAsync();

    private static IQueryable<LeaseGroup> ApplyScope(IQueryable<LeaseGroup> query, SpaceAccessScope? scope)
    {
        if (scope == null || scope.HasGlobalAccess) return query;

        var roots = scope.RootPaths.ToList();
        return query.Where(group => roots.Any(root => group.Space.Path.StartsWith(root)));
    }
}
