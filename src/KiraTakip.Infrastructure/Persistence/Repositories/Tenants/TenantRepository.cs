using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Tenants;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Dtos.Document;

namespace KiraTakip.Repositories.Tenants;

public class TenantRepository(ApplicationDbContext context)
    : RepositoryBase<Tenant>(context), ITenantRepository
{
    public async Task<List<TenantListItemDto>> GetListAsync(
        SpaceAccessScope? scope)
    {
        var query = ApplyScope(_dbSet.AsNoTracking(), scope);
        var roots = scope is { HasGlobalAccess: false } ? scope.RootPaths.ToList() : null;

        var now = DateTime.Now;
        return await query
            .OrderBy(tenant => tenant.TenantNo)
            .Select(tenant => new TenantListItemDto
            {
                Id = tenant.Id,
                TenantNo = tenant.TenantNo,
                DisplayName = tenant.Name,
                TaxNo = tenant.TaxNo,
                TenantCategoryName = tenant.TenantCategory != null ? tenant.TenantCategory.Name : null,
                Phone = tenant.Phone,
                Email = tenant.Email,
                RegistrationDate = tenant.RegistrationDate,
                ActiveLeaseCount = _ctx.Leases.Count(lease =>
                    lease.TenantId == tenant.Id
                    && lease.Status == LeaseStatus.Active
                    && lease.StartDate <= now
                    && lease.EndDate >= now
                    && (roots == null
                        || lease.Areas.Any(area => roots.Any(root => area.Space.Path.StartsWith(root)))))
            })
            .ToListAsync();
    }

    public async Task<PagedResult<TenantListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        SpaceAccessScope? scope)
    {
        var query = ApplyScope(_dbSet.AsNoTracking(), scope);
        var roots = scope is { HasGlobalAccess: false } ? scope.RootPaths.ToList() : null;

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(tenant =>
                EF.Functions.Like(tenant.TenantNo, $"%{search}%")
                || EF.Functions.Like(tenant.Name, $"%{search}%")
                || (tenant.TaxNo != null && EF.Functions.Like(tenant.TaxNo, $"%{search}%"))
                || EF.Functions.Like(tenant.Phone, $"%{search}%")
                || EF.Functions.Like(tenant.Email, $"%{search}%"));
        }

        var now = DateTime.Now;
        var itemsQuery = query
            .OrderBy(tenant => tenant.TenantNo)
            .ThenBy(tenant => tenant.Id)
            .Select(tenant => new TenantListItemDto
            {
                Id = tenant.Id,
                TenantNo = tenant.TenantNo,
                DisplayName = tenant.Name,
                TaxNo = tenant.TaxNo,
                TenantCategoryName = tenant.TenantCategory != null ? tenant.TenantCategory.Name : null,
                Phone = tenant.Phone,
                Email = tenant.Email,
                RegistrationDate = tenant.RegistrationDate,
                ActiveLeaseCount = _ctx.Leases.Count(lease =>
                    lease.TenantId == tenant.Id
                    && lease.Status == LeaseStatus.Active
                    && lease.StartDate <= now
                    && lease.EndDate >= now
                    && (roots == null
                        || lease.Areas.Any(area => roots.Any(root => area.Space.Path.StartsWith(root)))))
            });

        return await GetPagedResultAsync(query, itemsQuery, tableQuery);
    }

    public Task<bool> IsInScopeAsync(int tenantId, SpaceAccessScope? scope)
        => ApplyScope(_dbSet.AsNoTracking(), scope).AnyAsync(tenant => tenant.Id == tenantId);

    public Task<List<ReservationTenantOptionDto>> GetReservationOptionsAsync()
        => _dbSet.AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.TenantNo)
            .Select(tenant => new ReservationTenantOptionDto(tenant.Id, tenant.Name))
            .ToListAsync();

    public Task<TenantDetailsDto?> GetDetailsAsync(
        int id,
        SpaceAccessScope? scope = null)
        => ApplyScope(_dbSet.AsNoTracking(), scope)
            .Where(tenant => tenant.Id == id)
            .Select(tenant => new TenantDetailsDto
            {
                Id = tenant.Id,
                TenantCategoryId = tenant.TenantCategoryId,
                TenantCategoryName = tenant.TenantCategory != null ? tenant.TenantCategory.Name : null,
                SectorId = tenant.SectorId,
                SectorName = tenant.Sector != null ? tenant.Sector.Name : null,
                TenantNo = tenant.TenantNo,
                Name = tenant.Name,
                TradeRegistryNo = tenant.TradeRegistryNo,
                TaxNo = tenant.TaxNo,
                TaxOffice = tenant.TaxOffice,
                MersisNo = tenant.MersisNo,
                Phone = tenant.Phone,
                Email = tenant.Email,
                Address = tenant.Address,
                RegistrationDate = tenant.RegistrationDate
            })
            .FirstOrDefaultAsync();

    public Task<Tenant?> GetForUpdateAsync(
        int id,
        SpaceAccessScope? scope = null)
        => ApplyScope(_dbSet, scope)
            .FirstOrDefaultAsync(tenant => tenant.Id == id);

    public Task<bool> TenantNoExistsAsync(string tenantNo, int? excludeTenantId = null)
        => _dbSet
            .AsNoTracking()
            .AnyAsync(tenant => tenant.TenantNo == tenantNo
                && (excludeTenantId == null || tenant.Id != excludeTenantId));

    public Task<bool> TaxNoExistsAsync(string taxNo, int? excludeTenantId = null)
        => _dbSet
            .AsNoTracking()
            .AnyAsync(tenant => tenant.TaxNo == taxNo
                && (excludeTenantId == null || tenant.Id != excludeTenantId));

    public Task<List<string>> GetExistingTenantNosAsync()
        => _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(tenant => tenant.TenantNo)
            .ToListAsync();

    public Task<int?> GetCategoryIdAsync(int tenantId)
        => _dbSet.AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => (int?)tenant.TenantCategoryId)
            .FirstOrDefaultAsync();

    public async Task<bool> IsInactiveAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == tenantId, ct);

        return tenant is not null && !tenant.IsActive;
    }

    public Task<Tenant?> GetByIdIgnoreQueryFiltersAsync(int id, CancellationToken ct = default)
        => _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(tenant => tenant.Id == id, ct);

    public Task<Tenant?> GetActiveByIdAsync(int id, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(
            tenant => tenant.Id == id && tenant.IsActive,
            ct);
    public async Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int tenantId)
    {
        if (!await _dbSet.AsNoTracking().AnyAsync(tenant => tenant.Id == tenantId))
            return null;

        var spacePaths = await _ctx.Leases
            .AsNoTracking()
            .Where(lease => lease.TenantId == tenantId)
            .SelectMany(lease => lease.Areas)
            .Select(area => area.Space.Path)
            .Distinct()
            .ToListAsync();

        return new DocumentOwnerContextDto(tenantId, spacePaths);
    }

    public async Task<bool> HasHistoricalDependencyAsync(int tenantId)
        => await _ctx.Leases.IgnoreQueryFilters().AnyAsync(lease => lease.TenantId == tenantId)
            || await _ctx.Charges.IgnoreQueryFilters().AnyAsync(charge => charge.TenantId == tenantId)
            || await _ctx.Reservations.IgnoreQueryFilters().AnyAsync(reservation => reservation.TenantId == tenantId)
            || await _ctx.Users.IgnoreQueryFilters().AnyAsync(user => user.TenantId == tenantId)
            || await _ctx.Roller.IgnoreQueryFilters().AnyAsync(role => role.TenantId == tenantId);

    private IQueryable<Tenant> ApplyScope(IQueryable<Tenant> query, SpaceAccessScope? scope)
    {
        if (scope == null || scope.HasGlobalAccess)
            return query;

        var roots = scope.RootPaths.ToList();
        var authorizedTenantIds = _ctx.Leases
            .Where(lease => lease.Areas.Any(area => roots.Any(root => area.Space.Path.StartsWith(root))))
            .Select(lease => lease.TenantId)
            .Distinct();

        return query.Where(tenant => authorizedTenantIds.Contains(tenant.Id));
    }
}
