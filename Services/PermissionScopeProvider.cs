using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PermissionScopeProvider : IPermissionScopeProvider
{
    private bool _globalAccess = true;
    private List<int> _propertyIds = new();
    private List<int> _unitIds = new();

    public bool GlobalAccess => _globalAccess;
    public IReadOnlyList<int> AccessiblePropertyIds => _propertyIds;
    public IReadOnlyList<int> AccessibleUnitIds => _unitIds;

    public bool IsInScope(int propertyId) =>
        _globalAccess || _propertyIds.Contains(propertyId);

    public void PropertyGuard(int propertyId)
    {
        if (!IsInScope(propertyId))
            throw new UnauthorizedAccessException($"Property {propertyId} is out of permission scope.");
    }

    public void Initialize(UserScopeDto dto)
    {
        _globalAccess = dto.GlobalAccess;
        _propertyIds = dto.PropertyIds;
        _unitIds = dto.UnitIds;
    }
}
