using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PermissionScopeProvider : IPermissionScopeProvider
{
    private List<int> _propertyIds = [];
    private List<int> _unitIds = [];

    public bool GlobalAccess { get; private set; } = true;
    public IReadOnlyList<int> AccessiblePropertyIds => _propertyIds;
    public IReadOnlyList<int> AccessibleUnitIds => _unitIds;
    public bool IsInScope(int propertyId) => GlobalAccess || _propertyIds.Contains(propertyId);

    public void Initialize(UserScopeDto dto)
    {
        GlobalAccess = dto.GlobalAccess;
        _propertyIds = dto.PropertyIds;
        _unitIds = dto.UnitIds;
    }
}
