using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TenantRepository(ApplicationDbContext context)
    : BaseRepository<Tenant>(context), ITenantRepository
{
    public async Task<List<TenantListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        IQueryable<Tenant> query = _dbSet.AsNoTracking();

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            var authorizedTenantIds = _ctx.Leases
                .Where(lease => propertyIds.Contains(lease.Unit.PropertyId)
                    || unitIds.Contains(lease.UnitId))
                .Select(lease => lease.TenantId)
                .Distinct();

            query = query.Where(tenant => authorizedTenantIds.Contains(tenant.Id));
        }

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
                RegistrationDate = tenant.RegistrationDate
            })
            .ToListAsync();
    }

    public Task<bool> IsInScopeAsync(
        int tenantId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        if (authorizedPropertyIds == null && authorizedUnitIds == null)
            return _dbSet.AsNoTracking().AnyAsync(tenant => tenant.Id == tenantId);

        var propertyIds = authorizedPropertyIds ?? [];
        var unitIds = authorizedUnitIds ?? [];
        return _ctx.Leases.AsNoTracking().AnyAsync(lease =>
            lease.TenantId == tenantId
            && (propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId)));
    }

    public Task<List<ReservationTenantOptionDto>> GetReservationOptionsAsync()
        => _dbSet.AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.TenantNo)
            .Select(tenant => new ReservationTenantOptionDto(tenant.Id, tenant.Name))
            .ToListAsync();

    public Task<TenantDetailsDto?> GetDetailsAsync(
        int id,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
        => ApplyScope(_dbSet.AsNoTracking(), authorizedPropertyIds, authorizedUnitIds)
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
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
        => ApplyScope(_dbSet, authorizedPropertyIds, authorizedUnitIds)
            .FirstOrDefaultAsync(tenant => tenant.Id == id);

    public Task<bool> TenantNoExistsAsync(string tenantNo, int? excludeTenantId = null)
        => _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(tenant => tenant.TenantNo == tenantNo
                && (excludeTenantId == null || tenant.Id != excludeTenantId));

    public Task<bool> TaxNoExistsAsync(string taxNo, int? excludeTenantId = null)
        => _dbSet
            .IgnoreQueryFilters()
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
            .Select(tenant => tenant.TenantCategoryId)
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

        var leaseScopes = await _ctx.Leases
            .AsNoTracking()
            .Where(lease => lease.TenantId == tenantId)
            .Select(lease => new { lease.Unit.PropertyId, lease.UnitId })
            .ToListAsync();

        return new DocumentOwnerContextDto(
            tenantId,
            leaseScopes.Select(scope => scope.PropertyId).Distinct().ToList(),
            leaseScopes.Select(scope => scope.UnitId).Distinct().ToList());
    }

    private IQueryable<Tenant> ApplyScope(
        IQueryable<Tenant> query,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds)
    {
        if (authorizedPropertyIds == null && authorizedUnitIds == null)
            return query;

        var propertyIds = authorizedPropertyIds ?? [];
        var unitIds = authorizedUnitIds ?? [];
        var authorizedTenantIds = _ctx.Leases
            .Where(lease => propertyIds.Contains(lease.Unit.PropertyId)
                || unitIds.Contains(lease.UnitId))
            .Select(lease => lease.TenantId)
            .Distinct();

        return query.Where(tenant => authorizedTenantIds.Contains(tenant.Id));
    }
}
