using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class SystemSettingRepository(ApplicationDbContext context)
    : RepositoryBase<SystemSetting>(context), ISystemSettingRepository
{
    public Task<List<SystemSetting>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(setting => setting.IsActive && keys.Contains(setting.Key))
            .ToListAsync(cancellationToken);

    public Task<List<SystemSetting>> GetActiveListAsync(
        CancellationToken cancellationToken = default)
        => _dbSet.AsNoTracking()
            .Where(setting => setting.IsActive)
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken);
}
