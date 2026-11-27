using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Services.Identity;

public class PermissionScopeProvider : IPermissionScopeProvider
{
    public bool GlobalAccess { get; private set; } = true;
    public SpaceAccessScope SpaceScope { get; private set; } = new(true, []);

    public bool IsSpaceInScope(string spacePath) => SpaceScope.Includes(spacePath);

    public void Initialize(UserScopeDto dto)
    {
        GlobalAccess = dto.GlobalAccess;
        SpaceScope = new(dto.GlobalAccess, dto.SpaceRootPaths);
    }
}
