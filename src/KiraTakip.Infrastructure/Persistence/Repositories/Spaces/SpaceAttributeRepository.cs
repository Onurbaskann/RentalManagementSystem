using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceAttributeRepository(ApplicationDbContext ctx)
    : RepositoryBase<SpaceAttributeDefinition>(ctx), ISpaceAttributeRepository
{
    public Task<List<SpaceAttributeDefinition>> GetAllOrderedAsync()
        => _dbSet.AsNoTracking().OrderBy(d => d.Name).ToListAsync();

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
        => _dbSet.AsNoTracking().AnyAsync(d => d.Code == code && (excludeId == null || d.Id != excludeId));

    public async Task<bool> IsUsedAsync(int definitionId)
        => await ctx.SpaceAttributeValues.AsNoTracking().AnyAsync(v => v.AttributeDefinitionId == definitionId)
           || await ctx.SpaceTypeDefaultAttributes.AsNoTracking().AnyAsync(d => d.AttributeDefinitionId == definitionId);

    public Task<List<SpaceAttributeDefinition>> GetByIdsAsync(IReadOnlyCollection<int> ids)
        => _dbSet.Where(d => ids.Contains(d.Id)).ToListAsync();

    public Task<List<SpaceAttributeDefinition>> GetTypeDefaultsAsync(int spaceTypeId)
        => ctx.SpaceTypeDefaultAttributes.AsNoTracking()
            .Where(d => d.SpaceTypeId == spaceTypeId)
            .Select(d => d.AttributeDefinition)
            .OrderBy(d => d.Name)
            .ToListAsync();

    public Task<List<SpaceAttributeValue>> GetValuesForUpdateAsync(int spaceId)
        => ctx.SpaceAttributeValues.Include(v => v.AttributeDefinition).Where(v => v.SpaceId == spaceId).ToListAsync();

    public void AddValue(SpaceAttributeValue value) => ctx.SpaceAttributeValues.Add(value);
}
