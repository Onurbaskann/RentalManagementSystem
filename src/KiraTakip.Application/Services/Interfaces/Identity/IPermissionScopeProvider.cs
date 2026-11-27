using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces.Identity;

public interface IPermissionScopeProvider
{
    bool GlobalAccess { get; }
    SpaceAccessScope SpaceScope { get; }
    bool IsSpaceInScope(string spacePath);
    void Initialize(UserScopeDto dto);
}
