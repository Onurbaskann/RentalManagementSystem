using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces.Identity;

public interface IPermissionScopeCache
{
    Task<UserScopeDto> GetAsync(string userId);
    void Invalidate(string userId);
    void InvalidateMany(IEnumerable<string> userIds);
}
