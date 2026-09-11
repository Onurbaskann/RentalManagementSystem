using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Settings;

public interface ISystemSettingRepository : IRepositoryBase<SystemSetting>
{
    Task<List<SystemSetting>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    Task<List<SystemSetting>> GetActiveListAsync(CancellationToken cancellationToken = default);
}
