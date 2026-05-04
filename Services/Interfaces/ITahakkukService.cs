using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface ITahakkukService
{
    Task<List<KiraTahakkuk>> GetAllAsync(int? sozlesmeId = null, string? userId = null);
    Task<KiraTahakkuk?> GetByIdAsync(int id);
    Task<(bool Basarili, string? Hata)> OlusturAsync(int sozlesmeId, DateTime donemBaslangic);
    Task GecikmeleriGuncelleAsync();
    Task OdenenTutarGuncelleAsync(int tahakkukId);
}
