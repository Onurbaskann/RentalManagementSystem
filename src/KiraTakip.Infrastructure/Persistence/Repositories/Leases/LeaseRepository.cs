using System.Linq.Expressions;
using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Leases;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Repositories.Leases;

public class LeaseRepository(ApplicationDbContext ctx) : RepositoryBase<Lease>(ctx), ILeaseRepository
{
    private const string NamePathSeparator = " / ";

    private static readonly LeaseStatus[] TenantVisibleStatuses =
        [LeaseStatus.Active, LeaseStatus.Ended, LeaseStatus.Terminated];

    private static readonly Expression<Func<Lease, LeaseListItemDto>> ListItemProjection = lease => new LeaseListItemDto
    {
        Id = lease.Id,
        LeaseNo = lease.LeaseNo,
        TenantId = lease.TenantId,
        TenantDisplayName = lease.Tenant.DisplayName,
        TenantCategoryName = lease.Tenant.TenantCategory != null ? lease.Tenant.TenantCategory.Name : string.Empty,
        Area = lease.Areas
            .OrderBy(area => area.Id)
            .Select(area => new LeaseAreaDto
            {
                SpaceId = area.SpaceId,
                SpaceName = area.Space.Name,
                SpaceIdPath = area.Space.Path,
                SpaceArea = area.Space.Area,
                RentedArea = area.RentedArea,
                SnapshotCode = area.SnapshotCode,
                SnapshotName = area.SnapshotName,
                SnapshotTypeName = area.SnapshotTypeName,
                SnapshotUsage = area.SnapshotUsage,
                SnapshotArea = area.SnapshotArea,
                SnapshotPath = area.SnapshotPath
            })
            .FirstOrDefault(),
        StartDate = lease.StartDate,
        EndDate = lease.EndDate,
        MonthlyAmount = 0,
        IsRentFree = lease.IsRentFree,
        Status = lease.Status
    };

    public async Task<List<LeaseListItemDto>> GetListAsync(string? filter, SpaceAccessScope? scope)
    {
        var now = DateTime.Now;
        var query = ApplyScope(_dbSet.AsNoTracking(), scope);

        query = filter switch
        {
            "aktif" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now),
            "surek" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30)),
            "gecmis" => query.Where(s => s.Status == LeaseStatus.Ended),
            "feshedildi" => query.Where(s => s.Status == LeaseStatus.Terminated),
            "onaybekliyor" => query.Where(s => s.Status == LeaseStatus.Draft),
            "revizyon" => query.Where(s => s.Status == LeaseStatus.RevisionRequested),
            "tum" => query,
            _ => query
        };

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(ListItemProjection)
            .ToListAsync();
    }

    public Task<LeaseDetailDto?> GetDetailsAsync(int id)
        => ProjectDetails(_dbSet.AsNoTracking().Where(lease => lease.Id == id))
            .FirstOrDefaultAsync();

    public Task<LeaseDetailDto?> GetTenantDetailsAsync(
        int leaseId,
        int tenantId,
        SpaceAccessScope? scope = null)
    {
        var query = _dbSet.AsNoTracking().Where(lease =>
            lease.Id == leaseId
            && lease.TenantId == tenantId
            && TenantVisibleStatuses.Contains(lease.Status));

        return ProjectDetails(ApplyScope(query, scope)).FirstOrDefaultAsync();
    }

    public async Task<List<LeaseListItemDto>> GetTenantPortalListAsync(
        int tenantId,
        SpaceAccessScope? scope = null)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(lease =>
                lease.TenantId == tenantId
                && TenantVisibleStatuses.Contains(lease.Status)),
            scope);

        return await query
            .OrderByDescending(lease => lease.StartDate)
            .Select(ListItemProjection)
            .ToListAsync();
    }

    public async Task<PagedResult<LeaseListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        string? filter,
        SpaceAccessScope? scope)
    {
        var now = DateTime.Now;
        var query = ApplyScope(_dbSet.AsNoTracking(), scope);

        query = filter switch
        {
            "aktif" => query.Where(lease => lease.Status == LeaseStatus.Active && lease.StartDate <= now && lease.EndDate >= now),
            "surek" => query.Where(lease => lease.Status == LeaseStatus.Active && lease.StartDate <= now && lease.EndDate >= now && lease.EndDate <= now.AddDays(30)),
            "gecmis" => query.Where(lease => lease.Status == LeaseStatus.Ended),
            "feshedildi" => query.Where(lease => lease.Status == LeaseStatus.Terminated),
            "onaybekliyor" => query.Where(lease => lease.Status == LeaseStatus.Draft),
            "revizyon" => query.Where(lease => lease.Status == LeaseStatus.RevisionRequested),
            _ => query.Where(lease => lease.Status != LeaseStatus.Terminated)
        };

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(lease =>
                EF.Functions.Like(lease.Tenant.Name, $"%{search}%")
                || lease.Areas.Any(area =>
                    EF.Functions.Like(area.SnapshotPath, $"%{search}%")
                    || EF.Functions.Like(area.Space.Name, $"%{search}%"))
                || EF.Functions.Like(lease.LeaseNo, $"%{search}%"));
        }

        var itemsQuery = query
            .OrderByDescending(lease => lease.StartDate)
            .ThenByDescending(lease => lease.Id)
            .Select(ListItemProjection);

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<PagedResult<LeaseListItemDto>> GetTenantPortalPagedListAsync(
        int tenantId,
        TableQuery tableQuery,
        SpaceAccessScope? scope = null)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(lease =>
                lease.TenantId == tenantId
                && TenantVisibleStatuses.Contains(lease.Status)),
            scope);

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(lease =>
                lease.Areas.Any(area =>
                    EF.Functions.Like(area.SnapshotPath, $"%{search}%")
                    || EF.Functions.Like(area.Space.Name, $"%{search}%"))
                || EF.Functions.Like(lease.LeaseNo, $"%{search}%"));
        }

        var itemsQuery = query
            .OrderByDescending(lease => lease.StartDate)
            .ThenByDescending(lease => lease.Id)
            .Select(ListItemProjection);

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    private static IQueryable<LeaseDetailDto> ProjectDetails(IQueryable<Lease> query)
        => query.Select(s => new LeaseDetailDto
        {
            Id = s.Id,
            LeaseNo = s.LeaseNo,
            TenantId = s.TenantId,
            TenantDisplayName = s.Tenant.DisplayName,
            TenantPhone = s.Tenant.Phone,
            TenantEmail = s.Tenant.Email,
            TenantCategoryId = s.Tenant.TenantCategoryId,
            TenantCategoryName = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Name : string.Empty,
            Area = s.Areas
                .OrderBy(area => area.Id)
                .Select(area => new LeaseAreaDto
                {
                    SpaceId = area.SpaceId,
                    SpaceName = area.Space.Name,
                    SpaceIdPath = area.Space.Path,
                    SpaceArea = area.Space.Area,
                    RentedArea = area.RentedArea,
                    SnapshotCode = area.SnapshotCode,
                    SnapshotName = area.SnapshotName,
                    SnapshotTypeName = area.SnapshotTypeName,
                    SnapshotUsage = area.SnapshotUsage,
                    SnapshotArea = area.SnapshotArea,
                    SnapshotPath = area.SnapshotPath
                })
                .FirstOrDefault(),
            LeaseGroupId = s.LeaseGroupId,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Description = s.Description,
            Status = s.Status,
            TerminationDate = s.TerminationDate,
            TerminationReason = s.TerminationReason,
            IsVatApplied = s.IsKdvApplied,
            IsRentFree = s.IsRentFree,
            DueDateRuleType = s.DueDateRuleType,
            DueDay = s.DueDay,
            ActivityLog = s.ActivityLog
                    .OrderByDescending(ig => ig.TransactionDate)
                    .Select(ig => new LeaseActivityLogDto
                    {
                        Id = ig.Id,
                        TransactionDate = ig.TransactionDate,
                        ActivityType = ig.ActivityType,
                        Description = ig.Description,
                        OldRentAmount = ig.OldRentAmount,
                        NewRentAmount = ig.NewRentAmount,
                        OldEndDate = ig.OldEndDate,
                        NewEndDate = ig.NewEndDate,
                        InflationRate = ig.InflationRate,
                        IsKdvApplied = ig.IsKdvApplied ?? false,
                        KdvRate = ig.KdvRate,
                        KdvAmount = ig.KdvAmount,
                        KdvIncludedAmount = ig.KdvIncludedAmount
                    }).ToList(),
            LeaseRateOverrides = s.LeaseRateOverrides
                    .Select(st => new LeaseRateDto
                    {
                        Id = st.Id,
                        ChargeTypeId = st.ChargeTypeId,
                        ChargeTypeCode = st.ChargeType.Code,
                        ChargeTypeName = st.ChargeType.Name,
                        ChargeTypeBehavior = st.ChargeType.Behavior,
                        UnitValue = st.UnitValue,
                        CalculationMethod = st.CalculationMethod,
                        VatRate = st.KdvRate
                    }).ToList()
        });

    public async Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId, SpaceAccessScope? scope = null)
    {
        var query = ApplyScope(_dbSet.AsNoTracking().Where(s => s.TenantId == tenantId), scope);

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(ListItemProjection)
            .ToListAsync();
    }

    public async Task<List<LeaseListItemDto>> GetBySpaceIdAsync(int spaceId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.Areas.Any(area => area.SpaceId == spaceId))
            .OrderByDescending(s => s.StartDate)
            .Select(ListItemProjection)
            .ToListAsync();
    }

    public Task<int> CountActiveByTenantAsync(
        int tenantId,
        DateTime currentTime,
        SpaceAccessScope? scope = null)
        => ApplyScope(
                _dbSet.AsNoTracking().Where(lease =>
                    lease.TenantId == tenantId
                    && lease.Status == LeaseStatus.Active
                    && lease.StartDate <= currentTime
                    && lease.EndDate >= currentTime),
                scope)
            .CountAsync();

    public Task<int> GetTerminatedCountAsync(SpaceAccessScope? scope)
        => ApplyScope(
                _dbSet.AsNoTracking().Where(lease => lease.Status == LeaseStatus.Terminated),
                scope)
            .CountAsync();

    public async Task<List<Lease>> GetAktiflerAsync()
        => await _dbSet
            .Include(s => s.Tenant)
            .Include(s => s.Areas).ThenInclude(area => area.Space)
            .Where(s => s.Status == LeaseStatus.Active)
            .OrderBy(s => s.Tenant.Name)
            .ToListAsync();

    public async Task<(int SpaceId, string SpacePath, int TenantCategoryId)?> GetSpaceAndCategoryAsync(int leaseId)
    {
        var info = await _dbSet.AsNoTracking()
            .Where(s => s.Id == leaseId)
            .Select(s => new
            {
                s.Tenant.TenantCategoryId,
                Area = s.Areas
                    .OrderBy(area => area.Id)
                    .Select(area => new { area.SpaceId, area.Space.Path })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
        return info?.Area == null
            ? null
            : (info.Area.SpaceId, info.Area.Path, info.TenantCategoryId);
    }

    public async Task<Dictionary<int, (int SpaceId, string SpacePath, int TenantCategoryId)>> GetSpaceAndCategoriesAsync(
        IReadOnlyCollection<int> leaseIds)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(s => leaseIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Tenant.TenantCategoryId,
                Area = s.Areas
                    .OrderBy(area => area.Id)
                    .Select(area => new { area.SpaceId, area.Space.Path })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return rows
            .Where(row => row.Area != null)
            .ToDictionary(row => row.Id, row => (row.Area!.SpaceId, row.Area.Path, row.TenantCategoryId));
    }

    public async Task<List<LeaseDropdownDto>> GetActiveDropdownAsync(SpaceAccessScope? scope)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(lease => lease.Status == LeaseStatus.Active),
            scope);

        return await query
            .OrderBy(s => s.Tenant.Name)
            .Select(s => new LeaseDropdownDto
            {
                Id = s.Id,
                SpaceId = s.Areas.OrderBy(area => area.Id).Select(area => area.SpaceId).FirstOrDefault(),
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                SpacePath = s.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotPath).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<List<LeaseSpaceOptionDto>> GetActiveLeaseSpacesByTenantIdAsync(
        int tenantId,
        SpaceAccessScope? scope = null,
        CancellationToken ct = default)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(s => s.TenantId == tenantId && s.Status == LeaseStatus.Active),
            scope);

        var rows = await query
            .SelectMany(s => s.Areas)
            .Select(area => new
            {
                Id = area.SpaceId,
                area.Space.Name,
                area.Space.Area,
                area.Space.IsShared,
                area.Space.Path
            })
            .Distinct()
            .ToListAsync(ct);

        var names = await LoadAncestorNamesAsync(rows.Select(row => row.Path));
        return rows
            .Select(row => new LeaseSpaceOptionDto(row.Id, row.Name, BuildNamePath(row.Path, row.Name, names), row.Area, row.IsShared))
            .OrderBy(option => option.NamePath, StringComparer.CurrentCulture)
            .ToList();
    }

    public Task<bool> IsTenantInScopeAsync(int tenantId, SpaceAccessScope? scope)
        => scope == null || scope.HasGlobalAccess
            ? ctx.Tenants.AsNoTracking().AnyAsync(tenant => tenant.Id == tenantId)
            : ApplyScope(_dbSet.AsNoTracking().Where(lease => lease.TenantId == tenantId), scope).AnyAsync();

    public async Task<LeaseSpaceContextDto?> GetSpaceContextAsync(int spaceId)
    {
        var row = await ctx.Spaces.AsNoTracking()
            .Where(space => space.Id == spaceId)
            .Select(space => new
            {
                space.Id,
                space.Code,
                space.Name,
                TypeName = space.SpaceType.Name,
                space.Usage,
                space.Area,
                space.Path,
                space.IsShared,
                space.IsActive
            })
            .FirstOrDefaultAsync();
        if (row == null) return null;

        var names = await LoadAncestorNamesAsync([row.Path]);
        return new LeaseSpaceContextDto(
            row.Id,
            row.Code,
            row.Name,
            row.TypeName,
            row.Usage,
            row.Area,
            row.Path,
            BuildNamePath(row.Path, row.Name, names),
            row.IsShared,
            row.IsActive);
    }

    public async Task<List<LeaseSpaceOptionDto>> GetSelectableSpacesAsync(
        bool sharedOnly,
        SpaceAccessScope? scope,
        int? includeSpaceId = null)
    {
        var query = ctx.Spaces.AsNoTracking().Where(space =>
            (space.IsActive && space.Usage == SpaceUsage.Rentable && space.IsShared == sharedOnly)
            || (includeSpaceId.HasValue && space.Id == includeSpaceId.Value));

        if (scope is { HasGlobalAccess: false })
        {
            var roots = scope.RootPaths.ToList();
            query = query.Where(space =>
                space.Id == includeSpaceId || roots.Any(root => space.Path.StartsWith(root)));
        }

        var rows = await query
            .Select(space => new { space.Id, space.Name, space.Path, space.Area, space.IsShared })
            .ToListAsync();
        var names = await LoadAncestorNamesAsync(rows.Select(row => row.Path));

        return rows
            .Select(row => new LeaseSpaceOptionDto(
                row.Id,
                row.Name,
                BuildNamePath(row.Path, row.Name, names),
                row.Area,
                row.IsShared))
            .OrderBy(option => option.NamePath, StringComparer.CurrentCulture)
            .ToList();
    }

    private async Task<Dictionary<int, string>> LoadAncestorNamesAsync(IEnumerable<string> paths)
    {
        var ids = paths
            .SelectMany(SpaceTreePolicy.GetAncestorIds)
            .Distinct()
            .ToList();
        if (ids.Count == 0) return [];

        return await ctx.Spaces.AsNoTracking()
            .Where(space => ids.Contains(space.Id))
            .ToDictionaryAsync(space => space.Id, space => space.Name);
    }

    private static string BuildNamePath(string path, string ownName, IReadOnlyDictionary<int, string> ancestorNames)
        => string.Join(
            NamePathSeparator,
            SpaceTreePolicy.GetAncestorIds(path)
                .Where(ancestorNames.ContainsKey)
                .Select(id => ancestorNames[id])
                .Append(ownName));

    public Task<bool> HasOverlappingActiveLeaseAsync(
        int spaceId,
        DateTime startDate,
        DateTime endDate,
        int? excludeLeaseId,
        int? excludeLeaseGroupId = null)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        return _dbSet.AsNoTracking().AnyAsync(lease =>
            lease.Areas.Any(area => area.SpaceId == spaceId)
            && !lease.IsDeleted
            && (lease.Status == LeaseStatus.Active
                || lease.Status == LeaseStatus.Draft
                || lease.Status == LeaseStatus.RevisionRequested)
            && (!excludeLeaseId.HasValue || lease.Id != excludeLeaseId.Value)
            && (!excludeLeaseGroupId.HasValue || lease.LeaseGroupId != excludeLeaseGroupId.Value)
            && lease.StartDate.Date <= end
            && lease.EndDate.Date >= start);
    }

    public async Task<LeaseDraftEditDto?> GetDraftForEditAsync(int leaseId, SpaceAccessScope? scope)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(lease =>
                lease.Id == leaseId
                && (lease.Status == LeaseStatus.Draft
                    || lease.Status == LeaseStatus.RevisionRequested)),
            scope);

        var draft = await query.Select(lease => new LeaseDraftEditDto
        {
            LeaseId = lease.Id,
            LeaseNo = lease.LeaseNo,
            SpaceId = lease.Areas.OrderBy(area => area.Id).Select(area => area.SpaceId).FirstOrDefault(),
            TenantId = lease.TenantId,
            SpacePath = lease.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotPath).FirstOrDefault() ?? string.Empty,
            SpaceTypeNameSnapshot = lease.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotTypeName).FirstOrDefault() ?? string.Empty,
            SpaceUsageSnapshot = lease.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotUsage).FirstOrDefault(),
            SpaceAreaSnapshot = lease.Areas.OrderBy(area => area.Id).Select(area => area.SnapshotArea).FirstOrDefault(),
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            DueDateRuleType = lease.DueDateRuleType,
            DueDay = lease.DueDay,
            Description = lease.Description,
            IsRentFree = lease.IsRentFree,
            RentedArea = lease.Areas.OrderBy(area => area.Id).Select(area => (decimal?)area.RentedArea).FirstOrDefault(),
            Status = lease.Status,
            RowVersion = lease.RowVersion,
            OwnerUserId = lease.CreatedBy,
            CreatedAt = lease.CreatedAt,
            UpdatedAt = lease.UpdatedAt,
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
        }).FirstOrDefaultAsync();

        if (draft == null) return null;

        draft.OwnerDisplayName = await _ctx.Users
            .AsNoTracking()
            .Where(user => user.Id == draft.OwnerUserId)
            .Select(user => user.AdSoyad ?? user.UserName ?? user.Email ?? user.Id)
            .FirstOrDefaultAsync() ?? draft.OwnerUserId;
        draft.LatestRevision = await _ctx.SozlesmeIncelemeGecmisleri
            .AsNoTracking()
            .Where(history => history.LeaseId == leaseId
                && history.ActionType == LeaseReviewActionType.RevisionRequested)
            .OrderByDescending(history => history.ActionDate)
            .ThenByDescending(history => history.Id)
            .Select(history => new LeaseReviewHistoryDto(
                history.Id,
                history.ActionType,
                history.FromStatus,
                history.ToStatus,
                history.Explanation,
                history.ActorUser.AdSoyad
                    ?? history.ActorUser.UserName
                    ?? history.ActorUser.Email
                    ?? history.ActorUserId,
                history.ActionDate))
            .FirstOrDefaultAsync();

        return draft;
    }

    public Task<Lease?> GetForDecisionAsync(int leaseId, SpaceAccessScope? scope)
        => ApplyScope(
                _dbSet
                    .Include(lease => lease.Areas)
                    .Where(lease => lease.Id == leaseId
                        && (lease.Status == LeaseStatus.Draft
                            || lease.Status == LeaseStatus.RevisionRequested)),
                scope)
            .FirstOrDefaultAsync();

    public Task<bool> HasOpenApplicationForSpaceAsync(
        int spaceId,
        int? excludedLeaseId = null,
        int? excludedLeaseGroupId = null)
        => _dbSet.AsNoTracking().AnyAsync(lease =>
            lease.Areas.Any(area => area.SpaceId == spaceId)
            && (!excludedLeaseId.HasValue || lease.Id != excludedLeaseId.Value)
            && (!excludedLeaseGroupId.HasValue || lease.LeaseGroupId != excludedLeaseGroupId.Value)
            && (lease.Status == LeaseStatus.Draft
                || lease.Status == LeaseStatus.RevisionRequested));

    public Task<bool> HasChargesAsync(int leaseId)
        => _ctx.Charges.AsNoTracking().AnyAsync(charge => charge.LeaseId == leaseId);

    public Task<bool> HasUnsettledChargesAsync(int leaseId)
        => _ctx.Charges.AsNoTracking().AnyAsync(charge =>
            charge.LeaseId == leaseId
            && charge.Status != ChargeStatus.Paid
            && charge.Status != ChargeStatus.Cancelled);

    public Task<bool> HasCreationActivityAsync(int leaseId)
        => _ctx.SozlesmeIslemGecmisleri.AsNoTracking().AnyAsync(activity =>
            activity.LeaseId == leaseId
            && activity.ActivityType == LeaseActivityType.Creation);

    public Task<Lease?> GetDeletedApplicationForAuditAsync(int leaseId)
        => _dbSet.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(lease =>
            lease.Id == leaseId
            && lease.IsDeleted
            && (lease.Status == LeaseStatus.Draft
                || lease.Status == LeaseStatus.RevisionRequested));

    public Task<List<int>> GetExpiredActiveLeaseIdsAsync(DateTime today, int batchSize)
        => _dbSet.AsNoTracking()
            .Where(lease => !lease.IsDeleted
                && lease.Status == LeaseStatus.Active
                && lease.EndDate.Date < today.Date)
            .OrderBy(lease => lease.Id)
            .Select(lease => lease.Id)
            .Take(batchSize)
            .ToListAsync();

    public Task<Lease?> GetForExpirationAsync(int leaseId)
        => _dbSet.FirstOrDefaultAsync(lease => lease.Id == leaseId && !lease.IsDeleted);

    public Task<Lease?> GetWithActivityLogAsync(int leaseId)
        => _dbSet
            .Include(lease => lease.ActivityLog)
            .Include(lease => lease.Areas).ThenInclude(area => area.Space)
            .FirstOrDefaultAsync(lease => lease.Id == leaseId);

    public async Task<LeaseOwnerContextDto?> GetDocumentOwnerContextAsync(
        int leaseId,
        bool tenantPortalOnly = false)
    {
        var context = await _dbSet
            .AsNoTracking()
            .Where(lease => lease.Id == leaseId
                && (!tenantPortalOnly || TenantVisibleStatuses.Contains(lease.Status)))
            .Select(lease => new
            {
                lease.TenantId,
                SpacePaths = lease.Areas.Select(area => area.Space.Path).ToList()
            })
            .FirstOrDefaultAsync();

        return context == null
            ? null
            : new LeaseOwnerContextDto(context.TenantId, context.SpacePaths);
    }

    public Task<List<string>> GetExistingLeaseNosAsync()
        => _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(lease => lease.LeaseNo)
            .ToListAsync();

    public async Task<decimal?> GetRentedAreaAsync(int leaseId)
        => await ctx.LeaseAreas
            .AsNoTracking()
            .Where(area => area.LeaseId == leaseId)
            .OrderBy(area => area.Id)
            .Select(area => (decimal?)area.RentedArea)
            .FirstOrDefaultAsync();

    public async Task<Dictionary<int, decimal>> GetRentedAreasAsync(IReadOnlyCollection<int> leaseIds)
    {
        var ids = leaseIds.ToList();
        return await ctx.LeaseAreas
            .AsNoTracking()
            .Where(area => ids.Contains(area.LeaseId))
            .GroupBy(area => area.LeaseId)
            .Select(group => new { LeaseId = group.Key, Rented = group.Sum(area => area.RentedArea) })
            .ToDictionaryAsync(row => row.LeaseId, row => row.Rented);
    }

    public async Task ReplaceLeaseAreasAsync(
        int leaseId,
        IReadOnlyCollection<(LeaseSpaceContextDto Space, decimal RentedArea)> areas)
    {
        var existing = await ctx.LeaseAreas.Where(area => area.LeaseId == leaseId).ToListAsync();
        var targetSpaceIds = areas.Select(item => item.Space.SpaceId).ToHashSet();
        foreach (var removed in existing.Where(area => !targetSpaceIds.Contains(area.SpaceId)))
            ctx.LeaseAreas.Remove(removed);

        foreach (var (space, rentedArea) in areas)
        {
            var row = existing.FirstOrDefault(area => area.SpaceId == space.SpaceId);
            if (row == null)
            {
                row = new LeaseArea { LeaseId = leaseId, SpaceId = space.SpaceId };
                ctx.LeaseAreas.Add(row);
            }

            row.RentedArea = rentedArea;
            row.SnapshotCode = space.Code;
            row.SnapshotName = space.Name;
            row.SnapshotTypeName = space.TypeName;
            row.SnapshotUsage = space.Usage;
            row.SnapshotArea = space.Area;
            row.SnapshotPath = space.NamePath;
        }
    }

    private static IQueryable<Lease> ApplyScope(IQueryable<Lease> query, SpaceAccessScope? scope)
    {
        if (scope == null || scope.HasGlobalAccess) return query;

        var roots = scope.RootPaths.ToList();
        return query.Where(lease => lease.Areas.Any(area =>
            roots.Any(root => area.Space.Path.StartsWith(root))));
    }
}
