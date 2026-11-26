using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceRateRepository(ApplicationDbContext ctx) : RepositoryBase<SpaceRate>(ctx), ISpaceRateRepository
{
    public Task<List<SpaceRate>> GetBySpaceAsync(int spaceId)
        => _dbSet.AsNoTracking().Where(r => r.SpaceId == spaceId)
            .OrderBy(r => r.TenantCategoryId).ThenBy(r => r.ChargeTypeId).ToListAsync();

    public Task<SpaceRate?> GetByKeyAsync(int spaceId, int tenantCategoryId, int chargeTypeId)
        => _dbSet.FirstOrDefaultAsync(r => r.SpaceId == spaceId
                                           && r.TenantCategoryId == tenantCategoryId
                                           && r.ChargeTypeId == chargeTypeId);
}
