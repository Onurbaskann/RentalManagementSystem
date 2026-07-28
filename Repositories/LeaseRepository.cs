using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Constants;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class LeaseRepository : BaseRepository<Lease>, ILeaseRepository
{
    public LeaseRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<LeaseListItemDto>> GetListAsync(
        string? filter,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lease =>
                propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId));
        }

        query = filter switch
        {
            "aktif" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now),
            "surek" => query.Where(s => s.Status == LeaseStatus.Active && s.StartDate <= now && s.EndDate >= now && s.EndDate <= now.AddDays(30)),
            "gecmis" => query.Where(s => s.Status == LeaseStatus.Ended),
            "feshedildi" => query.Where(s => s.Status == LeaseStatus.Terminated),
            _ => query
        };

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(s => new LeaseListItemDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                TenantCategoryName = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Name : string.Empty,
                UnitId = s.UnitId,
                UnitName = s.Unit.Name,
                PropertyId = s.Unit.PropertyId,
                PropertyName = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                MonthlyAmount = 0,
                Status = s.Status,
                UnitArea = s.Unit.Area
            })
            .ToListAsync();
    }

    public Task<LeaseDetailDto?> GetDetailsAsync(int id)
        => ProjectDetails(_dbSet.AsNoTracking().Where(lease => lease.Id == id))
            .FirstOrDefaultAsync();

    public Task<LeaseDetailDto?> GetTenantDetailsAsync(
        int leaseId,
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().Where(lease =>
            lease.Id == leaseId && lease.TenantId == tenantId);
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lease =>
                propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId));
        }

        return ProjectDetails(query).FirstOrDefaultAsync();
    }

    private static IQueryable<LeaseDetailDto> ProjectDetails(IQueryable<Lease> query)
        => query.Select(s => new LeaseDetailDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                TenantPhone = s.Tenant.Phone,
                TenantEmail = s.Tenant.Email,
                TenantCategoryId = s.Tenant.TenantCategoryId,
                TenantCategoryName = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Name : string.Empty,
                UnitId = s.UnitId,
                UnitName = s.Unit.Name,
                UnitNo = s.Unit.UnitNo,
                UnitFloorNo = s.Unit.FloorNo,
                UnitArea = s.Unit.Area,
                UnitStructure = s.Unit.Property.UnitStructure,
                PropertyId = s.Unit.PropertyId,
                PropertyName = s.Unit.Property.Name,
                PropertyCity = s.Unit.Property.City,
                PropertyDistrict = s.Unit.Property.District,
                PropertyNeighborhood = s.Unit.Property.Neighborhood,
                PropertyAddress = s.Unit.Property.Address,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Description = s.Description,
                Status = s.Status,
                TerminationDate = s.TerminationDate,
                TerminationReason = s.TerminationReason,
                IsVatApplied = s.IsKdvApplied,
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

    public async Task<List<LeaseListItemDto>> GetByTenantIdAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lease =>
                propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId));
        }

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(s => new LeaseListItemDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                TenantCategoryName = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Name : string.Empty,
                UnitId = s.UnitId,
                UnitName = s.Unit.Name,
                PropertyId = s.Unit.PropertyId,
                PropertyName = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                MonthlyAmount = 0,
                Status = s.Status,
                UnitArea = s.Unit.Area
            })
            .ToListAsync();
    }

    public async Task<List<LeaseListItemDto>> GetByUnitIdAsync(int unitId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.UnitId == unitId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new LeaseListItemDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                TenantCategoryName = s.Tenant.TenantCategory != null ? s.Tenant.TenantCategory.Name : string.Empty,
                UnitId = s.UnitId,
                UnitName = s.Unit.Name,
                PropertyId = s.Unit.PropertyId,
                PropertyName = s.Unit.Property.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                MonthlyAmount = 0,
                Status = s.Status,
                UnitArea = s.Unit.Area
            })
            .ToListAsync();
    }

    public Task<int> CountActiveByTenantAsync(
        int tenantId,
        DateTime currentTime,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().Where(lease =>
            lease.TenantId == tenantId
            && lease.Status == LeaseStatus.Active
            && lease.StartDate <= currentTime
            && lease.EndDate >= currentTime);
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lease =>
                propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId));
        }

        return query.CountAsync();
    }

    public async Task<List<Lease>> GetAktiflerAsync()
        => await _dbSet
            .Include(s => s.Tenant)
            .Include(s => s.Unit).ThenInclude(b => b.Property)
            .Where(s => s.Status == LeaseStatus.Active)
            .OrderBy(s => s.Tenant.Name)
            .ToListAsync();

    public async Task<(int TasinmazId, int? KategoriId)?> GetPropertyAndCategoryAsync(int leaseId)
    {
        var info = await _dbSet.AsNoTracking()
            .Where(s => s.Id == leaseId)
            .Select(s => new { s.Unit.PropertyId, s.Tenant.TenantCategoryId })
            .FirstOrDefaultAsync();
        return info == null ? null : (info.PropertyId, info.TenantCategoryId);
    }

    public async Task<List<LeaseDropdownDto>> GetActiveDropdownAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(lease => lease.Status == LeaseStatus.Active);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(lease =>
                propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId));
        }

        return await query
            .OrderBy(s => s.Tenant.Name)
            .Select(s => new LeaseDropdownDto
            {
                Id = s.Id,
                UnitId = s.UnitId,
                TenantId = s.TenantId,
                TenantDisplayName = s.Tenant.DisplayName,
                UnitName = s.Unit.Name,
                PropertyName = s.Unit.Property.Name
            })
            .ToListAsync();
    }

    public async Task<List<UnitLookupDto>> GetActiveLeaseUnitsByTenantIdAsync(int tenantId, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == LeaseStatus.Active)
            .Select(s => new UnitLookupDto
            {
                Id = s.UnitId,
                Name = s.Unit.Name,
                PropertyName = s.Unit.Property.Name,
                UnitNo = s.Unit.UnitNo,
            })
            .Distinct()
            .OrderBy(b => b.PropertyName).ThenBy(b => b.Name)
            .ToListAsync(ct);
    }

    public Task<bool> HasActiveLeaseForUnitAsync(int unitId, DateTime currentTime)
        => _dbSet.AnyAsync(lease =>
            lease.UnitId == unitId
            && lease.Status == LeaseStatus.Active
            && lease.StartDate <= currentTime
            && lease.EndDate >= currentTime);

    public Task<Lease?> GetWithActivityLogAsync(int leaseId)
        => _dbSet
            .Include(lease => lease.ActivityLog)
            .Include(lease => lease.Unit)
            .FirstOrDefaultAsync(lease => lease.Id == leaseId);

    public async Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int leaseId)
    {
        var context = await _dbSet
            .AsNoTracking()
            .Where(lease => lease.Id == leaseId)
            .Select(lease => new
            {
                lease.TenantId,
                lease.UnitId,
                lease.Unit.PropertyId
            })
            .FirstOrDefaultAsync();

        return context == null
            ? null
            : new DocumentOwnerContextDto(
                context.TenantId,
                [context.PropertyId],
                [context.UnitId]);
    }
}
