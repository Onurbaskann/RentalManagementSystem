using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Services.Interfaces;

public interface ISystemSettingService
{
    Task<PagedResult<SystemSettingListItemDto>> GetPagedAsync(
        TableQuery query,
        CancellationToken cancellationToken = default);
    Task<SystemSettingListItemDto?> GetByIdAsync(
        GetSystemSettingInput input,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(
        UpdateSystemSettingInput input,
        CancellationToken cancellationToken = default);
}
