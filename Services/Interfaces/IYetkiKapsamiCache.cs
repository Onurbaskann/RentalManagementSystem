using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IYetkiKapsamiCache
{
    Task<KullaniciKapsamDto> GetAsync(string userId);
    void Invalidate(string userId);
    void InvalidateMany(IEnumerable<string> userIds);
}
