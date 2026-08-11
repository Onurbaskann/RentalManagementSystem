using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ChargeTypeRepository : RepositoryBase<ChargeType>, IChargeTypeRepository
{
    public ChargeTypeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ChargeTypeLookupDto>> GetManualChargeTypesAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.IsActive && b.Behavior == ChargeTypeBehavior.UserManual)
            .OrderBy(b => b.SortOrder)
            .Select(b => new ChargeTypeLookupDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Behavior = b.Behavior
            })
            .ToListAsync();

    public async Task<ChargeType?> GetActiveManualByIdAsync(int id)
        => await _dbSet
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && b.Behavior == ChargeTypeBehavior.UserManual);

    public async Task<List<ChargeTypeListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .Select(b => new ChargeTypeListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Behavior = b.Behavior,
                SortOrder = b.SortOrder,
                IsSystem = b.IsSystem,
                IsActive = b.IsActive
            })
            .ToListAsync();

    public async Task<int> GetMaxSortOrderAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.SortOrder) ?? 0;

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Code == code && (excludeId == null || b.Id != excludeId));

    public async Task<List<ChargeTypeLookupDto>> GetRezervasyonAdaylariAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.Behavior == ChargeTypeBehavior.ReservationSpecific && b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new ChargeTypeLookupDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                Behavior = b.Behavior
            })
            .ToListAsync();

    public Task<PagedResult<ChargeTypeListItemDto>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(type => type.Name.Contains(search) || type.Code.Contains(search));
        }
        if (Enum.TryParse<ChargeTypeBehavior>(tableQuery.Source, out var behavior))
            query = query.Where(type => type.Behavior == behavior);

        var items = query
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Name)
            .ThenBy(type => type.Id)
            .Select(type => new ChargeTypeListItemDto
            {
                Id = type.Id,
                Name = type.Name,
                Code = type.Code,
                Behavior = type.Behavior,
                SortOrder = type.SortOrder,
                IsSystem = type.IsSystem,
                IsActive = type.IsActive
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }

    public Task<bool> IsActiveReservationSpecificAsync(int id)
        => _dbSet.AsNoTracking().AnyAsync(type =>
            type.Id == id &&
            type.IsActive &&
            type.Behavior == ChargeTypeBehavior.ReservationSpecific);

    public Task<List<ChargeType>> GetActiveGenerationTypesAsync()
        => _dbSet.AsNoTracking()
            .Where(type => type.IsActive && (type.Behavior == ChargeTypeBehavior.MonthlyFixed || type.Behavior == ChargeTypeBehavior.FirstMonthOneTime))
            .OrderBy(type => type.SortOrder)
            .ToListAsync();

    public Task<List<ChargeType>> GetPricingMatrixTypesAsync()
        => _dbSet.AsNoTracking()
            .Where(type => type.Behavior != ChargeTypeBehavior.UserManual && type.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(type => type.SortOrder)
            .ToListAsync();

    public async Task<ChargeType?> ResolveReservationTypeAsync(int? preferredChargeTypeId)
    {
        if (preferredChargeTypeId.HasValue)
        {
            var preferred = await _dbSet.FirstOrDefaultAsync(type => type.Id == preferredChargeTypeId.Value && type.IsActive);
            if (preferred != null) return preferred;
        }

        return await _dbSet.FirstOrDefaultAsync(type => type.Behavior == ChargeTypeBehavior.ReservationSpecific && type.IsActive);
    }
}
