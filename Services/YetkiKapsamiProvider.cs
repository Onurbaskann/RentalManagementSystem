using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class YetkiKapsamiProvider : IYetkiKapsamiProvider
{
    private bool _globalErisim = true;
    private List<int> _tasinmazIds = new();
    private List<int> _birimIds = new();

    public bool GlobalErisim => _globalErisim;
    public IReadOnlyList<int> ErisilebilirTasinmazIds => _tasinmazIds;
    public IReadOnlyList<int> ErisilebilirBirimIds => _birimIds;

    public bool KapsamdaMi(int tasinmazId) =>
        _globalErisim || _tasinmazIds.Contains(tasinmazId);

    public void TasinmazGuard(int tasinmazId)
    {
        if (!KapsamdaMi(tasinmazId))
            throw new UnauthorizedAccessException($"Taşınmaz {tasinmazId} yetki kapsamı dışında.");
    }

    public void Initialize(KullaniciKapsamDto dto)
    {
        _globalErisim = dto.GlobalErisim;
        _tasinmazIds = dto.TasinmazIds;
        _birimIds = dto.BirimIds;
    }
}
