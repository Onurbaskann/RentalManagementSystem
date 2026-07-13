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

    public async Task<List<LeaseListItemDto>> GetListAsync(string? filter, List<int>? authorizedPropertyIds)
    {
        var now = DateTime.Now;
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (authorizedPropertyIds != null)
        {
            query = query.Where(s => authorizedPropertyIds.Contains(s.Unit.PropertyId));
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

    public async Task<LeaseDetailDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new LeaseDetailDto
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
                IsKdvApplied = s.IsKdvApplied,
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
                        KdvRate = st.KdvRate
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
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

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds)
    {
        var ids = leaseIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal?>();

        var kalemler = await _ctx.ChargeLineItems
            .Where(k => k.Charge.LeaseId.HasValue
                && ids.Contains(k.Charge.LeaseId.Value)
                && k.ChargeType.Code == BorcTipiConsts.Depozito
                && k.Charge.Status != ChargeStatus.Cancelled)
            .Select(k => new
            {
                SozlesmeId = k.Charge.LeaseId!.Value,
                Donem = k.Charge.PeriodStart,
                Amount = k.TotalAmount
            })
            .ToListAsync();

        return kalemler
            .GroupBy(x => x.SozlesmeId)
            .ToDictionary(g => g.Key, g => (decimal?)g.OrderBy(x => x.Donem).First().Amount);
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

    public async Task<List<LeaseDropdownDto>> GetAktifDropdownAsync()
        => await _dbSet.AsNoTracking()
            .Where(s => s.Status == LeaseStatus.Active)
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
