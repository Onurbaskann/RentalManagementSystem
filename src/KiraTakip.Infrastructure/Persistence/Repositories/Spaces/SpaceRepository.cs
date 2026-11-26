using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceRepository(ApplicationDbContext ctx) : RepositoryBase<Space>(ctx), ISpaceRepository
{
    public Task<Space?> GetNoTrackingAsync(int id)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<SpaceTreeRow>> GetRootRowsAsync()
        => ToRows(_dbSet.AsNoTracking().Where(s => s.ParentId == null)).ToListAsync();

    public Task<List<SpaceTreeRow>> GetTreeRowsAsync(string rootPath)
        => ToRows(_dbSet.AsNoTracking().Where(s => s.Path.StartsWith(rootPath))).ToListAsync();

    public Task<List<Space>> GetAncestorsAsync(string path)
    {
        var ids = SpaceTreePolicy.GetAncestorIds(path);
        return _dbSet.AsNoTracking().Where(s => ids.Contains(s.Id)).OrderBy(s => s.Depth).ToListAsync();
    }

    public Task<List<Space>> GetSubtreeForUpdateAsync(string path)
        => _dbSet.Where(s => s.Path.StartsWith(path)).OrderBy(s => s.Depth).ToListAsync();

    public Task<List<Space>> GetChildrenAsync(int parentId)
        => _dbSet.AsNoTracking().Where(s => s.ParentId == parentId).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();

    public Task<bool> CodeExistsAsync(int? parentId, string code, int? excludeId = null)
        => _dbSet.AsNoTracking().AnyAsync(s => s.ParentId == parentId && s.Code == code
                                               && (excludeId == null || s.Id != excludeId));

    public Task<bool> HasChildrenAsync(int spaceId)
        => _dbSet.AsNoTracking().AnyAsync(s => s.ParentId == spaceId);

    public Task<bool> AnyReservableAsync(IEnumerable<int> spaceIds)
    {
        var ids = spaceIds.ToList();
        return _dbSet.AsNoTracking().AnyAsync(s => ids.Contains(s.Id) && s.Usage == SpaceUsage.Reservable);
    }

    public Task<bool> HasReservableDescendantAsync(string path)
        => _dbSet.AsNoTracking().AnyAsync(s => s.Path.StartsWith(path) && s.Path != path && s.Usage == SpaceUsage.Reservable);

    public Task<bool> IsSpaceTypeUsedAsync(int spaceTypeId)
        => _dbSet.AsNoTracking().AnyAsync(s => s.SpaceTypeId == spaceTypeId);

    public Task<SpaceOpenRecords> GetOpenRecordsAsync(int spaceId)
        => Task.FromResult(SpaceOpenRecords.None);

    public Task<bool> HasHistoryAsync(int spaceId)
        => Task.FromResult(false);

    public void AddHistory(SpaceHistory history) => ctx.SpaceHistories.Add(history);

    public Task<List<SpaceHistoryDto>> GetHistoryAsync(int spaceId)
        => ctx.SpaceHistories.AsNoTracking()
            .Where(h => h.SpaceId == spaceId)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Select(h => new SpaceHistoryDto(h.Field, h.OldValue, h.NewValue, h.ChangedAt, h.UserId))
            .ToListAsync();

    public void AddAddress(SpaceAddress address) => ctx.SpaceAddresses.Add(address);

    public async Task<SpaceLinkedDefinitions> GetLinkedDefinitionsAsync(int spaceId)
        => new(
            await ctx.SpaceAddresses.FirstOrDefaultAsync(a => a.SpaceId == spaceId),
            await ctx.SpaceAttributeValues.Include(v => v.AttributeDefinition).Where(v => v.SpaceId == spaceId).ToListAsync(),
            await ctx.SpaceRates.Include(r => r.TenantCategory).Include(r => r.ChargeType).Where(r => r.SpaceId == spaceId).ToListAsync());

    private static IQueryable<SpaceTreeRow> ToRows(IQueryable<Space> query)
        => query.OrderBy(s => s.Depth).ThenBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => new SpaceTreeRow(
                s.Id, s.ParentId, s.Code, s.Name, s.SpaceType.Name, s.Usage, s.Area, s.IsActive, s.Depth, s.SortOrder));
}
