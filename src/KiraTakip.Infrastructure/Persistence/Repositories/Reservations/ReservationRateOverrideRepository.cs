using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Reservations;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Reservations;

public class ReservationRateOverrideRepository(ApplicationDbContext ctx) : RepositoryBase<ReservationRateOverride>(ctx), IReservationRateOverrideRepository
{
    public Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery tableQuery, SpaceAccessScope scope)
    {
        var query = _dbSet.AsNoTracking();
        if (!scope.HasGlobalAccess)
        {
            var roots = scope.RootPaths.ToList();
            query = query.Where(rate => roots.Any(path => rate.Space.Path.StartsWith(path)));
        }
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(rate => rate.Space.Name.Contains(search)
                || rate.ChargeType.Name.Contains(search));
        }
        var items = query
            .OrderBy(rate => rate.Space.Path)
            .ThenBy(rate => rate.Id)
            .Select(rate => new ReservationRateOverrideListItemDto
            {
                Id = rate.Id,
                SpaceId = rate.SpaceId,
                SpaceName = rate.Space.Name,
                SpacePath = rate.Space.Path,
                ChargeTypeId = rate.ChargeTypeId,
                ChargeTypeName = rate.ChargeType.Name,
                FreeDurationMinutes = rate.FreeDurationMinutes,
                BillingPeriodMinutes = rate.BillingPeriodMinutes,
                PeriodRate = rate.PeriodRate,
                KdvRate = rate.KdvRate,
                IsActive = rate.IsActive,
                Description = rate.Description
            });
        return GetPagedResultAsync(query, items, tableQuery);
    }

    public async Task<ReservationRateOverride?> GetActiveForSpaceAsync(
        int spaceId,
        string spacePath,
        bool includeSelf = true)
    {
        var ids = SpaceTreePolicy.GetAncestorIds(spacePath).ToList();
        if (includeSelf)
            ids.Add(spaceId);
        if (ids.Count == 0)
            return null;

        var rules = await _dbSet
            .Include(rate => rate.Space)
            .Where(rate => rate.IsActive && ids.Contains(rate.SpaceId))
            .ToListAsync();

        for (var i = ids.Count - 1; i >= 0; i--)
        {
            var rule = rules.FirstOrDefault(rate => rate.SpaceId == ids[i]);
            if (rule != null)
                return rule;
        }
        return null;
    }

    public async Task<ReservationRateOverride?> GetInheritedForSpaceAsync(int spaceId)
    {
        var path = await _ctx.Spaces.AsNoTracking()
            .Where(space => space.Id == spaceId)
            .Select(space => space.Path)
            .FirstOrDefaultAsync();
        return path == null ? null : await GetActiveForSpaceAsync(spaceId, path, includeSelf: false);
    }

    public Task<ReservationRateOverride?> GetForSpaceAsync(int spaceId)
        => _dbSet.FirstOrDefaultAsync(rate => rate.SpaceId == spaceId);

    public Task<ReservationRateOverride?> GetWithSpaceAsync(int id)
        => _dbSet.Include(rate => rate.Space)
            .FirstOrDefaultAsync(rate => rate.Id == id);

    public Task<bool> AnyActiveByChargeTypeAsync(int chargeTypeId)
        => _dbSet.AsNoTracking().AnyAsync(rate => rate.IsActive && rate.ChargeTypeId == chargeTypeId);

    public void Remove(ReservationRateOverride rate) => _dbSet.Remove(rate);
}
