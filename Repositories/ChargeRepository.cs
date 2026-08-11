using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ChargeRepository : RepositoryBase<Charge>, IChargeRepository
{
    public ChargeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public Task<List<PaymentPortalChargeDto>> GetPaymentPortalChargesAsync(
        int tenantId,
        DateTime dueDateLimit,
        CancellationToken cancellationToken = default)
        => _dbSet
            .AsNoTracking()
            .Where(charge => charge.TenantId == tenantId
                && charge.Status != ChargeStatus.Paid
                && charge.Status != ChargeStatus.Cancelled
                && charge.DueDate <= dueDateLimit
                && charge.TotalAmount > charge.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.Approved)
                    .Sum(allocation => allocation.Amount))
            .OrderBy(charge => charge.DueDate)
            .Select(charge => new PaymentPortalChargeDto(
                charge.Id,
                charge.Unit.Property.Name,
                charge.Unit.Name,
                charge.PeriodStart,
                charge.DueDate,
                charge.TotalAmount,
                charge.Allocations
                    .Where(allocation => allocation.Status == PaymentStatus.Approved)
                    .Sum(allocation => allocation.Amount)))
            .ToListAsync(cancellationToken);

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<ChargeListItemDto>> GetListAsync(int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking();

        if (leaseId.HasValue)
            q = q.Where(t => t.LeaseId == leaseId.Value);

        q = ApplyScope(q, authorizedPropertyIds, authorizedUnitIds);

        return await q.OrderByDescending(t => t.PeriodStart)
                      .Select(t => new ChargeListItemDto
                      {
                          Id = t.Id,
                          LeaseId = t.LeaseId,
                          TenantId = t.TenantId,
                          TenantDisplayName = t.Tenant.Name,
                          PropertyId = t.Unit.PropertyId,
                          PropertyName = t.Unit.Property.Name,
                          UnitId = t.UnitId,
                          UnitName = t.Unit.Name,
                          PeriodStart = t.PeriodStart,
                          DueDate = t.DueDate,
                          TotalAmount = t.TotalAmount,
                          PaidAmount = t.PaidAmount,
                          Status = t.Status,
                          SourceType = t.SourceType,
                          PendingPaymentCount = _ctx.PaymentAllocations.IgnoreQueryFilters()
                              .Count(o => o.ChargeId == t.Id && !o.IsDeleted && o.Status == PaymentStatus.PendingApproval),
                          LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                          {
                              ChargeTypeCode = k.ChargeType.Code,
                              ChargeTypeSortOrder = k.ChargeType.SortOrder,
                              ChargeTypeName = k.ChargeType.Name,
                              Description = k.Description,
                              CalculationMethod = k.CalculationMethod,
                              UnitValue = k.UnitValue,
                              Multiplier = k.Multiplier,
                              Amount = k.Amount,
                              KdvRate = k.KdvRate,
                              VatAmount = k.KdvAmount,
                              TotalAmount = k.TotalAmount,
                              SourceType = k.SourceType
                          }).ToList()
                      })
                      .ToListAsync();
    }

    // ── Sayfalı listeleme (DTO) ───────────────────────────────────────────
    public async Task<PagedResult<ChargeListItemDto>> GetPagedListAsync(TableQuery q, int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null)
    {
        IQueryable<Charge> query = _dbSet.AsNoTracking();

        if (leaseId.HasValue)
            query = query.Where(t => t.LeaseId == leaseId.Value);

        query = ApplyScope(query, authorizedPropertyIds, authorizedUnitIds);

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(t => EF.Functions.Like(t.Tenant.Name, $"%{s}%") ||
                                     EF.Functions.Like(t.Unit.Property.Name, $"%{s}%"));
        }

        if (q.From.HasValue) query = query.Where(t => t.DueDate >= q.From.Value);
        if (q.To.HasValue) query = query.Where(t => t.DueDate <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(t => t.TotalAmount >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(t => t.TotalAmount <= q.Max.Value);
        if (q.PropertyId.HasValue) query = query.Where(t => t.Unit.PropertyId == q.PropertyId.Value);
        if (q.UnitId.HasValue) query = query.Where(t => t.UnitId == q.UnitId.Value);
        if (q.TenantId.HasValue) query = query.Where(t => t.TenantId == q.TenantId.Value);
        if (q.Year.HasValue) query = query.Where(t => t.PeriodStart.Year == q.Year.Value);

        if (!string.IsNullOrWhiteSpace(q.Source))
        {
            ChargeSourceType? kt = q.Source.ToLower() switch
            {
                "manuel" => ChargeSourceType.Manual,
                "lease" => ChargeSourceType.Lease,
                "reservation" => ChargeSourceType.Reservation,
                _ => null
            };
            if (kt.HasValue) query = query.Where(t => t.SourceType == kt.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status != "tum")
        {
            if (q.Status == "odeme_onay")
            {
                query = query.Where(t => t.Allocations.Any(o =>
                    o.Status == PaymentStatus.PendingApproval &&
                    o.PaymentSourceType != PaymentSourceType.VirtualPos));
            }
            else if (q.Status == "iptal")
            {
                query = query.Where(t => t.Status == ChargeStatus.Cancelled);
            }
            else
            {
                ChargeStatus? d = q.Status.ToLower() switch
                {
                    "bekliyor" => ChargeStatus.Pending,
                    "kismi" => ChargeStatus.PartiallyPaid,
                    "tamodendi" => ChargeStatus.Paid,
                    "gecikti" => ChargeStatus.Overdue,
                    _ => null
                };
                if (d.HasValue) query = query.Where(t => t.Status == d.Value);
            }
        }
        else
        {
            query = query.Where(t => t.Status != ChargeStatus.Cancelled);
        }

        var itemsQuery = query.OrderByDescending(t => t.PeriodStart)
                              .ThenByDescending(t => t.Id)
                               .Select(t => new ChargeListItemDto
                               {
                                   Id = t.Id,
                                   LeaseId = t.LeaseId,
                                   TenantId = t.TenantId,
                                   TenantDisplayName = t.Tenant.Name,
                                   PropertyId = t.Unit.PropertyId,
                                   PropertyName = t.Unit.Property.Name,
                                   UnitId = t.UnitId,
                                   UnitName = t.Unit.Name,
                                   PeriodStart = t.PeriodStart,
                                   DueDate = t.DueDate,
                                   TotalAmount = t.TotalAmount,
                                   PaidAmount = t.PaidAmount,
                                   Status = t.Status,
                                   SourceType = t.SourceType,
                                   PendingPaymentCount = t.Allocations.Count(o => o.Status == PaymentStatus.PendingApproval),
                                   LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                                   {
                                       ChargeTypeCode = k.ChargeType.Code,
                                       ChargeTypeSortOrder = k.ChargeType.SortOrder,
                                       ChargeTypeName = k.ChargeType.Name,
                                       Description = k.Description,
                                       CalculationMethod = k.CalculationMethod,
                                       UnitValue = k.UnitValue,
                                       Multiplier = k.Multiplier,
                                       Amount = k.Amount,
                                       KdvRate = k.KdvRate,
                                       VatAmount = k.KdvAmount,
                                       TotalAmount = k.TotalAmount,
                                       SourceType = k.SourceType
                                   }).ToList()
                               });

        return await GetPagedResultAsync(query, itemsQuery, q);
    }

    public async Task<PagedResult<ChargeListItemDto>> GetTenantPagedListAsync(
        GetTenantChargeIndexInput input)
    {
        var filter = input.Query;
        var query = _dbSet
            .AsNoTracking()
            .Where(charge => charge.TenantId == input.TenantId
                && charge.Status != ChargeStatus.Cancelled);

        query = ApplyScope(
            query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(charge =>
                EF.Functions.Like(charge.Tenant.Name, $"%{search}%")
                || EF.Functions.Like(charge.Unit.Property.Name, $"%{search}%")
                || EF.Functions.Like(charge.Unit.Name, $"%{search}%"));
        }

        if (filter.UnitId.HasValue)
            query = query.Where(charge => charge.UnitId == filter.UnitId.Value);
        if (filter.Year.HasValue)
            query = query.Where(charge => charge.PeriodStart.Year == filter.Year.Value);

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            var sourceType = filter.Source switch
            {
                "manuel" => ChargeSourceType.Manual,
                "lease" => ChargeSourceType.Lease,
                "reservation" => ChargeSourceType.Reservation,
                _ => (ChargeSourceType?)null
            };
            if (sourceType.HasValue)
                query = query.Where(charge => charge.SourceType == sourceType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "tum")
        {
            query = filter.Status switch
            {
                "odeme_onay" => query.Where(charge => charge.Allocations.Any(payment =>
                    payment.Status == PaymentStatus.PendingApproval
                    && payment.PaymentSourceType != PaymentSourceType.VirtualPos)),
                "gecikti" => query.Where(charge => charge.DueDate < input.Today
                    && charge.TotalAmount > charge.PaidAmount),
                "tamodendi" => query.Where(charge => charge.PaidAmount >= charge.TotalAmount),
                "kismi" => query.Where(charge => charge.DueDate >= input.Today
                    && charge.PaidAmount > 0
                    && charge.PaidAmount < charge.TotalAmount),
                "bekliyor" => query.Where(charge => charge.DueDate >= input.Today
                    && charge.PaidAmount <= 0
                    && charge.TotalAmount > charge.PaidAmount),
                _ => query
            };
        }

        var itemsQuery = query
            .OrderByDescending(charge => charge.PeriodStart)
            .ThenByDescending(charge => charge.Id)
            .Select(charge => new ChargeListItemDto
            {
                Id = charge.Id,
                LeaseId = charge.LeaseId,
                TenantId = charge.TenantId,
                TenantDisplayName = charge.Tenant.Name,
                PropertyId = charge.Unit.PropertyId,
                PropertyName = charge.Unit.Property.Name,
                UnitId = charge.UnitId,
                UnitName = charge.Unit.Name,
                PeriodStart = charge.PeriodStart,
                DueDate = charge.DueDate,
                TotalAmount = charge.TotalAmount,
                PaidAmount = charge.PaidAmount,
                Status = charge.PaidAmount >= charge.TotalAmount
                    ? ChargeStatus.Paid
                    : charge.DueDate < input.Today
                        ? ChargeStatus.Overdue
                        : charge.PaidAmount > 0
                            ? ChargeStatus.PartiallyPaid
                            : ChargeStatus.Pending,
                SourceType = charge.SourceType,
                PendingPaymentCount = charge.Allocations.Count(payment =>
                    payment.Status == PaymentStatus.PendingApproval),
                PendingPaymentAmount = charge.Allocations
                    .Where(payment => payment.Status == PaymentStatus.PendingApproval)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0m,
                LineItems = charge.LineItems.Select(lineItem => new ChargeLineItemDto
                {
                    ChargeTypeCode = lineItem.ChargeType.Code,
                    ChargeTypeSortOrder = lineItem.ChargeType.SortOrder,
                    ChargeTypeName = lineItem.ChargeType.Name,
                    Description = lineItem.Description,
                    CalculationMethod = lineItem.CalculationMethod,
                    UnitValue = lineItem.UnitValue,
                    Multiplier = lineItem.Multiplier,
                    Amount = lineItem.Amount,
                    KdvRate = lineItem.KdvRate,
                    VatAmount = lineItem.KdvAmount,
                    TotalAmount = lineItem.TotalAmount,
                    SourceType = lineItem.SourceType
                }).ToList()
            });

        return await GetPagedResultAsync(query, itemsQuery, filter.Page, filter.Size);
    }

    // ── Detay (DTO) ───────────────────────────────────────────────────────
    public async Task<ChargeDetailDto?> GetDetailsAsync(int id)
    {
        return await CreateDetailsQuery()
            .Where(charge => charge.Id == id)
            .FirstOrDefaultAsync();
    }

    public Task<ChargeDetailDto?> GetTenantDetailsAsync(
        int chargeId,
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = CreateDetailsQuery().Where(charge =>
            charge.Id == chargeId && charge.TenantId == tenantId);
        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(charge =>
                charge.PropertyId.HasValue && propertyIds.Contains(charge.PropertyId.Value)
                || charge.UnitId.HasValue && unitIds.Contains(charge.UnitId.Value));
        }

        return query.FirstOrDefaultAsync();
    }
    private IQueryable<ChargeDetailDto> CreateDetailsQuery()
    {
        return _dbSet.AsNoTracking()
                           .Select(t => new ChargeDetailDto
                           {
                               Id = t.Id,
                               LeaseId = t.LeaseId,
                               TenantId = t.TenantId,
                               TenantDisplayName = t.Tenant.Name,
                               PropertyId = t.Unit.PropertyId,
                               PropertyName = t.Unit.Property.Name,
                               UnitId = t.UnitId,
                               UnitName = t.Unit.Name,
                               PeriodStart = t.PeriodStart,
                               PeriodEnd = t.PeriodEnd,
                               DueDate = t.DueDate,
                               ExpectedAmount = t.ExpectedAmount,
                               VatAmount = t.KdvAmount,
                               TotalAmount = t.TotalAmount,
                               PaidAmount = t.PaidAmount,
                               Status = t.Status,
                               SourceType = t.SourceType,
                               CreatedAt = t.CreatedAt,
                               LineItems = t.LineItems.Select(k => new ChargeLineItemDto
                               {
                                   ChargeTypeCode = k.ChargeType.Code,
                                   ChargeTypeSortOrder = k.ChargeType.SortOrder,
                                   ChargeTypeName = k.ChargeType.Name,
                                   Description = k.Description,
                                   CalculationMethod = k.CalculationMethod,
                                   UnitValue = k.UnitValue,
                                   Multiplier = k.Multiplier,
                                   Amount = k.Amount,
                                   KdvRate = k.KdvRate,
                                   VatAmount = k.KdvAmount,
                                   TotalAmount = k.TotalAmount,
                                   SourceType = k.SourceType
                               }).ToList(),
                               Allocations = t.Allocations.Select(o => new PaymentAllocationDto
                               {
                                   Id = o.Id,
                                   PaymentDate = o.PaymentDate,
                                   Amount = o.Amount,
                                   PaymentChannel = o.PaymentChannel,
                                   Status = o.Status,
                                   EntryDate = o.EntryDate,
                                   Description = o.Description,
                                   RejectionReason = o.RejectionReason
                               }).ToList()
                           });
    }

    public async Task<ChargeIndexOptionsDto> GetIndexOptionsAsync(GetChargeIndexOptionsInput input)
    {
        List<ChargePropertyFilterDto> properties;
        List<ChargeUnitFilterDto> units;

        var propertyIds = input.HasGlobalAccess
            ? null
            : input.PropertyIds?.ToList() ?? [];
        var unitIds = input.HasGlobalAccess
            ? null
            : input.UnitIds?.ToList() ?? [];

        if (!input.HasGlobalAccess)
        {
            var authorizedUnits = _ctx.Units.AsNoTracking()
                .Where(unit => propertyIds!.Contains(unit.PropertyId) || unitIds!.Contains(unit.Id));

            properties = await authorizedUnits
                .Select(unit => new ChargePropertyFilterDto(unit.PropertyId, unit.Property.Name))
                .Distinct()
                .OrderBy(property => property.Name)
                .ToListAsync();
            units = await authorizedUnits
                .OrderBy(unit => unit.PropertyId)
                .ThenBy(unit => unit.Name)
                .Select(unit => new ChargeUnitFilterDto(unit.Id, unit.Name, unit.PropertyId))
                .ToListAsync();
        }
        else
        {
            properties = await _ctx.Properties.AsNoTracking()
                .OrderBy(property => property.Name)
                .Select(property => new ChargePropertyFilterDto(property.Id, property.Name))
                .ToListAsync();
            units = await _ctx.Units.AsNoTracking()
                .OrderBy(unit => unit.PropertyId)
                .ThenBy(unit => unit.Name)
                .Select(unit => new ChargeUnitFilterDto(unit.Id, unit.Name, unit.PropertyId))
                .ToListAsync();
        }

        var optionCharges = ApplyScope(_dbSet.AsNoTracking(), propertyIds, unitIds);
        var tenantOptions = await optionCharges
            .Select(charge => new
            {
                Id = charge.TenantId,
                DisplayName = charge.Tenant.Name
            })
            .Distinct()
            .OrderBy(tenant => tenant.DisplayName)
            .ToListAsync();
        var tenants = tenantOptions
            .Select(tenant => new ChargeTenantFilterDto(tenant.Id, tenant.DisplayName))
            .ToList();
        var availableYears = await optionCharges
            .Select(charge => charge.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        var cancelledCount = 0;
        if (string.IsNullOrWhiteSpace(input.Status) || input.Status == "tum")
        {
            cancelledCount = await optionCharges
                .CountAsync(charge => charge.Status == ChargeStatus.Cancelled);
        }

        return new ChargeIndexOptionsDto(properties, units, tenants, availableYears, cancelledCount);
    }

    public async Task<TenantChargeOverviewDto> GetTenantChargeOverviewAsync(
        int tenantId,
        DateTime today,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = ApplyScope(
            _dbSet.AsNoTracking().Where(charge => charge.TenantId == tenantId),
            authorizedPropertyIds,
            authorizedUnitIds);
        var activeCharges = query.Where(charge => charge.Status != ChargeStatus.Cancelled);

        var totalChargeAmount = await activeCharges
            .SumAsync(charge => (decimal?)charge.TotalAmount) ?? 0m;
        var remainingDebtAmount = await activeCharges
            .Where(charge => charge.TotalAmount > charge.PaidAmount)
            .SumAsync(charge => (decimal?)(charge.TotalAmount - charge.PaidAmount)) ?? 0m;
        var overdueRemainingAmount = await activeCharges
            .Where(charge => charge.DueDate < today
                && charge.TotalAmount > charge.PaidAmount)
            .SumAsync(charge => (decimal?)(charge.TotalAmount - charge.PaidAmount)) ?? 0m;
        var availableYears = await query
            .Select(charge => charge.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        return new TenantChargeOverviewDto(
            totalChargeAmount,
            remainingDebtAmount,
            overdueRemainingAmount,
            availableYears);
    }
    public async Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input)
    {
        IQueryable<Charge> query = _dbSet
            .AsNoTracking()
            .Where(charge => charge.Status != ChargeStatus.Cancelled);

        query = ApplyScope(
            query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

        var availableYears = await query
            .Select(charge => charge.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

        var rows = await query
            .Where(charge => charge.PeriodStart.Year == input.Year)
            .GroupBy(charge => charge.PeriodStart.Month)
            .Select(group => new MonthlyCollectionReportRowDto
            {
                Month = group.Key,
                ChargeCount = group.Count(),
                ExpectedAmount = group.Sum(charge => charge.TotalAmount),
                CollectedAmount = group.Sum(charge => charge.PaidAmount),
                OverdueChargeCount = group.Count(charge =>
                    charge.DueDate < input.Today
                    && charge.Status != ChargeStatus.Paid
                    && charge.TotalAmount > charge.PaidAmount),
                OverdueAmount = group.Sum(charge =>
                    charge.DueDate < input.Today
                    && charge.Status != ChargeStatus.Paid
                    && charge.TotalAmount > charge.PaidAmount
                        ? charge.TotalAmount - charge.PaidAmount
                        : 0m)
            })
            .ToListAsync();

        return new MonthlyCollectionReportDto
        {
            Year = input.Year,
            Rows = rows,
            AvailableYears = availableYears
        };
    }

    // ── Manuel Borç — DTO ─────────────────────────────────────────────────
    public async Task<List<ManualChargeListItemDto>> GetManualChargeListAsync(
        List<int>? propertyIds,
        string? status = null,
        string? relation = null,
        int? leaseId = null,
        List<int>? unitIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual);

        q = ApplyScope(q, propertyIds, unitIds);

        if (leaseId.HasValue)
            q = q.Where(t => t.LeaseId == leaseId.Value);

        if (!string.IsNullOrWhiteSpace(relation))
        {
            if (relation == "sozlesmeli") q = q.Where(t => t.LeaseId != null);
            else if (relation == "sozlesmesiz") q = q.Where(t => t.LeaseId == null);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "tum")
        {
            if (status == "iptal")
                q = q.Where(t => t.Status == ChargeStatus.Cancelled);
            else
            {
                q = q.Where(t => t.Status != ChargeStatus.Cancelled);
                ChargeStatus? filteredStatus = status switch
                {
                    "bekliyor"  => ChargeStatus.Pending,
                    "kismi"     => ChargeStatus.PartiallyPaid,
                    "tamodendi" => ChargeStatus.Paid,
                    "gecikti"   => ChargeStatus.Overdue,
                    _           => null
                };
                if (filteredStatus.HasValue)
                    q = q.Where(t => t.Status == filteredStatus.Value);
            }
        }
        else
        {
            q = q.Where(t => t.Status != ChargeStatus.Cancelled);
        }

        return await q.OrderByDescending(t => t.CreatedAt)
                      .Select(t => new ManualChargeListItemDto
                      {
                          Id = t.Id,
                          LeaseId = t.LeaseId,
                          TenantId = t.TenantId,
                          TenantCategoryName = t.Tenant.TenantCategory != null ? t.Tenant.TenantCategory.Name : null,
                          TenantDisplayName = t.Tenant.Name,
                          PropertyName = t.Unit.Property.Name,
                          UnitName = t.Unit.Name,
                          ChargeTypeCode = t.LineItems
                              .OrderBy(k => k.ChargeType.SortOrder)
                              .Select(k => k.ChargeType.Code)
                              .FirstOrDefault(),
                          FirstLineItemDescription = t.LineItems
                              .OrderBy(k => k.ChargeType.SortOrder)
                              .Select(k => k.Description)
                              .FirstOrDefault(),
                          ExpectedAmount = t.ExpectedAmount,
                          VatAmount = t.KdvAmount,
                          TotalAmount = t.TotalAmount,
                          PaidAmount = t.PaidAmount,
                          DueDate = t.DueDate,
                          Status = t.Status,
                          CancellationNote = t.CancellationNote
                      })
                      .ToListAsync();
    }

    public async Task<int> GetCancelledManualChargeCountAsync(
        List<int>? propertyIds,
        List<int>? unitIds = null)
    {
        IQueryable<Charge> q = _dbSet.AsNoTracking()
            .Where(t => t.SourceType == ChargeSourceType.Manual && t.Status == ChargeStatus.Cancelled);

        q = ApplyScope(q, propertyIds, unitIds);

        return await q.CountAsync();
    }

    public async Task<PagedResult<ManualChargeListItemDto>> GetManualChargePagedListAsync(
        TableQuery tableQuery,
        List<int>? propertyIds,
        string? relation = null,
        int? leaseId = null,
        List<int>? unitIds = null)
    {
        IQueryable<Charge> query = _dbSet.AsNoTracking()
            .Where(charge => charge.SourceType == ChargeSourceType.Manual);

        query = ApplyScope(query, propertyIds, unitIds);

        if (leaseId.HasValue)
            query = query.Where(charge => charge.LeaseId == leaseId.Value);

        if (!string.IsNullOrWhiteSpace(relation))
        {
            if (relation == "sozlesmeli")
                query = query.Where(charge => charge.LeaseId != null);
            else if (relation == "sozlesmesiz")
                query = query.Where(charge => charge.LeaseId == null);
        }

        if (!string.IsNullOrWhiteSpace(tableQuery.Status) && tableQuery.Status != "tum")
        {
            if (tableQuery.Status == "iptal")
            {
                query = query.Where(charge => charge.Status == ChargeStatus.Cancelled);
            }
            else
            {
                query = query.Where(charge => charge.Status != ChargeStatus.Cancelled);
                var filteredStatus = tableQuery.Status switch
                {
                    "bekliyor" => ChargeStatus.Pending,
                    "kismi" => ChargeStatus.PartiallyPaid,
                    "tamodendi" => ChargeStatus.Paid,
                    "gecikti" => ChargeStatus.Overdue,
                    _ => (ChargeStatus?)null
                };
                if (filteredStatus.HasValue)
                    query = query.Where(charge => charge.Status == filteredStatus.Value);
            }
        }
        else
        {
            query = query.Where(charge => charge.Status != ChargeStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(charge =>
                EF.Functions.Like(charge.Tenant.Name, $"%{search}%")
                || EF.Functions.Like(charge.Unit.Property.Name, $"%{search}%")
                || EF.Functions.Like(charge.Unit.Name, $"%{search}%")
                || charge.LineItems.Any(lineItem =>
                    EF.Functions.Like(lineItem.Description, $"%{search}%")));
        }

        var itemsQuery = query
            .OrderByDescending(charge => charge.CreatedAt)
            .ThenByDescending(charge => charge.Id)
            .Select(charge => new ManualChargeListItemDto
            {
                Id = charge.Id,
                LeaseId = charge.LeaseId,
                TenantId = charge.TenantId,
                TenantCategoryName = charge.Tenant.TenantCategory != null
                    ? charge.Tenant.TenantCategory.Name
                    : null,
                TenantDisplayName = charge.Tenant.Name,
                PropertyName = charge.Unit.Property.Name,
                UnitName = charge.Unit.Name,
                ChargeTypeCode = charge.LineItems
                    .OrderBy(lineItem => lineItem.ChargeType.SortOrder)
                    .Select(lineItem => lineItem.ChargeType.Code)
                    .FirstOrDefault(),
                FirstLineItemDescription = charge.LineItems
                    .OrderBy(lineItem => lineItem.ChargeType.SortOrder)
                    .Select(lineItem => lineItem.Description)
                    .FirstOrDefault(),
                ExpectedAmount = charge.ExpectedAmount,
                VatAmount = charge.KdvAmount,
                TotalAmount = charge.TotalAmount,
                PaidAmount = charge.PaidAmount,
                DueDate = charge.DueDate,
                Status = charge.Status,
                CancellationNote = charge.CancellationNote
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public async Task<CurrentLeaseChargeDto> GetCurrentLeaseChargeAsync(GetCurrentLeaseChargeInput input)
    {
        var charge = await _dbSet
            .AsNoTracking()
            .Include(item => item.LineItems)
                .ThenInclude(lineItem => lineItem.ChargeType)
            .Where(item => item.LeaseId == input.LeaseId
                && item.Status != ChargeStatus.Cancelled
                && item.PeriodStart <= input.Today)
            .OrderByDescending(item => item.PeriodStart)
            .FirstOrDefaultAsync();

        var lineItems = charge?.LineItems
            .Where(lineItem => lineItem.ChargeType.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .OrderBy(lineItem => lineItem.ChargeType.SortOrder)
            .ToList() ?? [];

        return new CurrentLeaseChargeDto(charge?.PeriodStart, lineItems);
    }

    public async Task<TenantLeaseChargeDataDto> GetTenantLeaseDataAsync(
        GetTenantLeaseChargeDataInput input)
    {
        var propertyIds = input.PropertyIds?.ToList();
        var unitIds = input.UnitIds?.ToList();
        var charges = new List<ChargeListItemDto>();
        if (input.IncludeHistory)
        {
            charges = await _dbSet
                .AsNoTracking()
                .Where(charge => charge.TenantId == input.TenantId
                    && charge.LeaseId == input.LeaseId
                    && (propertyIds == null && unitIds == null
                        || propertyIds != null && propertyIds.Contains(charge.Unit.PropertyId)
                        || unitIds != null && unitIds.Contains(charge.UnitId)))
                .OrderByDescending(charge => charge.PeriodStart)
                .Select(charge => new ChargeListItemDto
                {
                    Id = charge.Id,
                    LeaseId = charge.LeaseId,
                    TenantId = charge.TenantId,
                    TenantDisplayName = charge.Tenant.Name,
                    PropertyId = charge.Unit.PropertyId,
                    PropertyName = charge.Unit.Property.Name,
                    UnitId = charge.UnitId,
                    UnitName = charge.Unit.Name,
                    PeriodStart = charge.PeriodStart,
                    DueDate = charge.DueDate,
                    TotalAmount = charge.TotalAmount,
                    PaidAmount = charge.PaidAmount,
                    Status = charge.Status == ChargeStatus.Cancelled
                        ? ChargeStatus.Cancelled
                        : charge.PaidAmount >= charge.TotalAmount
                            ? ChargeStatus.Paid
                            : charge.DueDate < input.Today
                                ? ChargeStatus.Overdue
                                : charge.PaidAmount > 0
                                    ? ChargeStatus.PartiallyPaid
                                    : ChargeStatus.Pending,
                    SourceType = charge.SourceType,
                    PendingPaymentCount = charge.Allocations.Count(payment =>
                        payment.Status == PaymentStatus.PendingApproval),
                    PendingPaymentAmount = charge.Allocations
                        .Where(payment => payment.Status == PaymentStatus.PendingApproval)
                        .Sum(payment => (decimal?)payment.Amount) ?? 0m,
                    LineItems = charge.LineItems.Select(lineItem => new ChargeLineItemDto
                    {
                        ChargeTypeCode = lineItem.ChargeType.Code,
                        ChargeTypeSortOrder = lineItem.ChargeType.SortOrder,
                        ChargeTypeName = lineItem.ChargeType.Name,
                        Description = lineItem.Description,
                        CalculationMethod = lineItem.CalculationMethod,
                        UnitValue = lineItem.UnitValue,
                        Multiplier = lineItem.Multiplier,
                        Amount = lineItem.Amount,
                        KdvRate = lineItem.KdvRate,
                        VatAmount = lineItem.KdvAmount,
                        TotalAmount = lineItem.TotalAmount,
                        SourceType = lineItem.SourceType
                    }).ToList()
                })
                .ToListAsync();
        }

        var currentCharge = await _dbSet
            .AsNoTracking()
            .Include(charge => charge.LineItems)
                .ThenInclude(lineItem => lineItem.ChargeType)
            .Where(charge => charge.TenantId == input.TenantId
                && charge.LeaseId == input.LeaseId
                && (propertyIds == null && unitIds == null
                    || propertyIds != null && propertyIds.Contains(charge.Unit.PropertyId)
                    || unitIds != null && unitIds.Contains(charge.UnitId))
                && charge.SourceType == ChargeSourceType.Lease
                && charge.Status != ChargeStatus.Cancelled
                && charge.PeriodStart <= input.Today)
            .OrderByDescending(charge => charge.PeriodStart)
            .FirstOrDefaultAsync();

        var currentLineItems = currentCharge?.LineItems
            .Where(lineItem => lineItem.ChargeType.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .OrderBy(lineItem => lineItem.ChargeType.SortOrder)
            .ToList() ?? [];

        return new TenantLeaseChargeDataDto(
            charges,
            new CurrentLeaseChargeDto(currentCharge?.PeriodStart, currentLineItems));
    }

    public async Task<ManualLeaseChargeSummaryDto> GetManualLeaseChargeSummaryAsync(
        GetManualLeaseChargeSummaryInput input)
    {
        var remainingAmounts = await _dbSet
            .AsNoTracking()
            .Where(charge => charge.LeaseId == input.LeaseId
                && charge.SourceType == ChargeSourceType.Manual
                && charge.Status != ChargeStatus.Cancelled)
            .Select(charge => charge.TotalAmount - charge.PaidAmount)
            .ToListAsync();

        return new ManualLeaseChargeSummaryDto(
            remainingAmounts.Count,
            remainingAmounts.Sum());
    }

    public async Task<TenantPanelChargeDataDto> GetTenantPanelDataAsync(
        GetTenantPanelChargeDataInput input)
    {
        var propertyIds = input.PropertyIds?.ToList();
        var unitIds = input.UnitIds?.ToList();
        var openCharges = ApplyScope(
            _dbSet.AsNoTracking().Where(charge =>
                charge.TenantId == input.TenantId
                && charge.Status != ChargeStatus.Cancelled
                && charge.TotalAmount > charge.PaidAmount),
            propertyIds,
            unitIds);

        var totalOutstandingDebt = 0m;
        var upcomingPaymentCount = 0;
        var upcomingPaymentAmount = 0m;
        var overdueCount = 0;
        var overdueAmount = 0m;
        var debtBalanceSparkline = new List<decimal>();
        var upcomingCharges = new List<TenantPanelUpcomingChargeDataDto>();

        if (input.IncludeDebtData)
        {
            totalOutstandingDebt = await openCharges
                .SumAsync(charge => (decimal?)(charge.TotalAmount - charge.PaidAmount)) ?? 0m;
            var upcomingPayments = await openCharges
                .Where(charge => charge.DueDate >= input.Today
                    && charge.DueDate <= input.Today.AddDays(7))
                .Select(charge => new { charge.TotalAmount, charge.PaidAmount })
                .ToListAsync();
            upcomingPaymentCount = upcomingPayments.Count;
            upcomingPaymentAmount = upcomingPayments.Sum(item => item.TotalAmount - item.PaidAmount);
            var overduePayments = await openCharges
                .Where(charge => charge.DueDate < input.Today)
                .Select(charge => new { charge.TotalAmount, charge.PaidAmount })
                .ToListAsync();
            overdueCount = overduePayments.Count;
            overdueAmount = overduePayments.Sum(item => item.TotalAmount - item.PaidAmount);

            for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
            {
                var monthEnd = new DateTime(input.Today.Year, input.Today.Month, 1)
                    .AddMonths(-monthOffset + 1).AddDays(-1);
                debtBalanceSparkline.Add(await openCharges
                    .Where(charge => charge.DueDate <= monthEnd)
                    .SumAsync(charge =>
                        (decimal?)(charge.TotalAmount - charge.PaidAmount)) ?? 0m);
            }

            upcomingCharges = await openCharges
                .OrderBy(charge => charge.DueDate)
                .Take(5)
                .Select(charge => new TenantPanelUpcomingChargeDataDto(
                    charge.Id,
                    charge.PeriodStart,
                    charge.DueDate,
                    charge.TotalAmount - charge.PaidAmount,
                    charge.Unit.Property.Name,
                    charge.Unit.Name))
                .ToListAsync();
        }

        var monthlyExpected = new List<TenantPanelMonthlyTotalDto>();
        if (input.IncludeMonthlyExpected)
        {
            var currentMonthStart = new DateTime(input.Today.Year, input.Today.Month, 1);
            var sixMonthStart = currentMonthStart.AddMonths(-5);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            var monthlyQuery = ApplyScope(
                _dbSet.AsNoTracking().Where(charge =>
                    charge.TenantId == input.TenantId
                    && charge.Status != ChargeStatus.Cancelled
                    && charge.DueDate >= sixMonthStart
                    && charge.DueDate < nextMonthStart),
                propertyIds,
                unitIds);
            monthlyExpected = await monthlyQuery
                .GroupBy(charge => new { charge.DueDate.Year, charge.DueDate.Month })
                .Select(group => new TenantPanelMonthlyTotalDto(
                    group.Key.Year,
                    group.Key.Month,
                    group.Sum(charge => charge.TotalAmount)))
                .ToListAsync();
        }

        return new TenantPanelChargeDataDto(
            totalOutstandingDebt,
            upcomingPaymentCount,
            upcomingPaymentAmount,
            overdueCount,
            overdueAmount,
            monthlyExpected,
            debtBalanceSparkline,
            upcomingCharges);
    }
    public async Task<Charge?> GetManualChargeByIdAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet
            .Include(t => t.Allocations)
            .Where(t => t.Id == id && t.SourceType == ChargeSourceType.Manual);

        query = ApplyScope(query, authorizedPropertyIds, authorizedUnitIds);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<Charge>> GetChargesToMarkOverdueAsync(DateTime today)
    {
        return await _dbSet.Where(t => t.Status != ChargeStatus.Paid &&
                                       t.Status != ChargeStatus.Cancelled &&
                                       t.DueDate < today)
                           .ToListAsync();
    }

    public async Task<List<Charge>> GetPendingReminderChargesAsync(
        GetPendingChargeRemindersInput input,
        CancellationToken cancellationToken)
    {
        IQueryable<Charge> query = _dbSet
            .Include(t => t.Tenant)
            .Include(t => t.Unit).ThenInclude(unit => unit.Property)
            .Include(t => t.Allocations)
            .Where(t => t.Status != ChargeStatus.Paid
                     && t.Status != ChargeStatus.Cancelled
                     && t.DueDate <= input.DueDateLimit);

        query = ApplyScope(
            query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

        return await query.ToListAsync(cancellationToken);
    }

    // ── Hesaplama ─────────────────────────────────────────────────────────
    public Task<bool> HasActiveForUnitTypeAsync(int unitTypeId)
        => _dbSet.AsNoTracking().AnyAsync(charge =>
            charge.Status != ChargeStatus.Paid
            && charge.Status != ChargeStatus.Cancelled
            && charge.Unit.UnitTypeId == unitTypeId);

    public Task<Charge?> GetByReservationWithAllocationsAsync(int reservationId)
        => _dbSet.Include(charge => charge.Allocations)
            .FirstOrDefaultAsync(charge => charge.ReservationId == reservationId);

    public Task<bool> ExistsForReservationAsync(int reservationId)
        => _dbSet.AnyAsync(charge => charge.ReservationId == reservationId);

    // ── Üretim yardımcıları ───────────────────────────────────────────────
    public async Task<List<Charge>> GetSilineceklerAsync(int leaseId, DateTime ilkGun)
        => await _dbSet.Where(t => t.LeaseId == leaseId
                                && t.PeriodStart >= ilkGun
                                && t.Status != ChargeStatus.Paid
                                && t.SourceType == ChargeSourceType.Lease
                                && !_ctx.PaymentAllocations.Any(o => o.ChargeId == t.Id))
                       .ToListAsync();

    public Task DeleteRangeAsync(IEnumerable<Charge> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    private static IQueryable<Charge> ApplyScope(
        IQueryable<Charge> query,
        List<int>? propertyIds,
        List<int>? unitIds)
    {
        if (propertyIds == null && unitIds == null)
            return query;

        if (propertyIds != null && unitIds != null)
        {
            return query.Where(charge =>
                propertyIds.Contains(charge.Unit.PropertyId)
                || unitIds.Contains(charge.UnitId));
        }

        if (propertyIds != null)
            return query.Where(charge => propertyIds.Contains(charge.Unit.PropertyId));

        return query.Where(charge => unitIds!.Contains(charge.UnitId));
    }
}
