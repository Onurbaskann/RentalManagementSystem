using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IManuelBorcService
{
    Task<List<KiraTahakkuk>> GetAllAsync(string? userId = null);
    Task<KiraTahakkuk?> GetByIdAsync(int id);
    Task<(bool Basarili, string? Hata, int TahakkukId)> CreateAsync(ManuelBorcCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int tahakkukId, string userId, string neden);
}
