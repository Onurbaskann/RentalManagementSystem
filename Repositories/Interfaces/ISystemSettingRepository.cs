using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface ISystemSettingRepository : IRepositoryBase<SystemSetting>
{
    Task<List<SystemSetting>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    Task<List<SystemSetting>> GetActiveListAsync(CancellationToken cancellationToken = default);
}
