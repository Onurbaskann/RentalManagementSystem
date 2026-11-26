using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceTypeRepository(ApplicationDbContext ctx) : RepositoryBase<SpaceType>(ctx), ISpaceTypeRepository
{
    public Task<List<SpaceType>> GetAllOrderedAsync()
        => _dbSet.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync();

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        => _dbSet.AsNoTracking().AnyAsync(t => t.Code == code && (excludeId == null || t.Id != excludeId));

    public Task<List<SpaceTypeDefaultAttribute>> GetDefaultsAsync(int spaceTypeId)
        => ctx.SpaceTypeDefaultAttributes.Where(d => d.SpaceTypeId == spaceTypeId).ToListAsync();

    public async Task<Dictionary<int, List<int>>> GetDefaultAttributeIdsAsync()
        => (await ctx.SpaceTypeDefaultAttributes.AsNoTracking()
                .Select(d => new { d.SpaceTypeId, d.AttributeDefinitionId }).ToListAsync())
            .GroupBy(d => d.SpaceTypeId)
            .ToDictionary(g => g.Key, g => g.Select(d => d.AttributeDefinitionId).ToList());

    public void AddDefault(SpaceTypeDefaultAttribute item) => ctx.SpaceTypeDefaultAttributes.Add(item);
}
