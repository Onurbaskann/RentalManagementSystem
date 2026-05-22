using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IDekontService
{
    Task<Dekont> EkleAsync(int odemeId, IFormFile dosya, string userId);
    Task<List<DekontListItemDto>> GetByOdemeIdAsync(int odemeId);
    Task<DekontDetayDto?> GetByIdAsync(int id);
    Task SilAsync(int id);
    string GetTamYol(string dosyaYolu);
}
