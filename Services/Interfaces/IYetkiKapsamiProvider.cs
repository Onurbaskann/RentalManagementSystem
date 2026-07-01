using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IYetkiKapsamiProvider
{
    bool GlobalErisim { get; }
    IReadOnlyList<int> ErisilebilirTasinmazIds { get; }
    IReadOnlyList<int> ErisilebilirBirimIds { get; }
    bool KapsamdaMi(int tasinmazId);
    void TasinmazGuard(int tasinmazId);
    void Initialize(KullaniciKapsamDto dto);
}
