using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPermissionScopeProvider
{
    bool GlobalErisim { get; }
    IReadOnlyList<int> ErisilebilirTasinmazIds { get; }
    IReadOnlyList<int> ErisilebilirBirimIds { get; }
    bool KapsamdaMi(int propertyId);
    void TasinmazGuard(int propertyId);
    void Initialize(KullaniciKapsamDto dto);
}
