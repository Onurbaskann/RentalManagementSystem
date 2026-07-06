using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PermissionScopeProvider : IPermissionScopeProvider
{
    private bool _globalErisim = true;
    private List<int> _tasinmazIds = new();
    private List<int> _birimIds = new();

    public bool GlobalErisim => _globalErisim;
    public IReadOnlyList<int> ErisilebilirTasinmazIds => _tasinmazIds;
    public IReadOnlyList<int> ErisilebilirBirimIds => _birimIds;

    public bool KapsamdaMi(int propertyId) =>
        _globalErisim || _tasinmazIds.Contains(propertyId);

    public void TasinmazGuard(int propertyId)
    {
        if (!KapsamdaMi(propertyId))
            throw new UnauthorizedAccessException($"Taşınmaz {propertyId} yetki kapsamı dışında.");
    }

    public void Initialize(KullaniciKapsamDto dto)
    {
        _globalErisim = dto.GlobalErisim;
        _tasinmazIds = dto.TasinmazIds;
        _birimIds = dto.BirimIds;
    }
}
