using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Settings;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Settings;

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
