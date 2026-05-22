using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IManuelBorcService
{
    // Listeleme — DTO döner (N+1 yok)
    Task<List<ManuelBorcListItemDto>> GetAllAsync(string? userId = null);

    // Create / Cancel — entity döner (business logic)
    Task<(bool Basarili, string? Hata, int TahakkukId)> CreateAsync(ManuelBorcCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int tahakkukId, string userId, string neden);

    // Dropdown verileri — DTO döner
    Task<List<SozlesmeDropdownDto>> GetAktifSozlesmelerAsync();
    Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync();
}
