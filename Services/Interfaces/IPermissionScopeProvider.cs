using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPermissionScopeProvider
{
    bool GlobalAccess { get; }
    IReadOnlyList<int> AccessiblePropertyIds { get; }
    IReadOnlyList<int> AccessibleUnitIds { get; }
    bool IsInScope(int propertyId);
    void PropertyGuard(int propertyId);
    void Initialize(UserScopeDto dto);
}
