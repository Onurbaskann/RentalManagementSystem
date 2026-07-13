using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IManualChargeService
{
    // Listeleme — DTO döner (N+1 yok)
    Task<List<ManuelBorcListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null, string? durum = null, string? baglanti = null, int? leaseId = null, IReadOnlyList<int>? birimIds = null);
    Task<int> GetIptalSayisiAsync(IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null);

    // Create / Cancel — entity döner (business logic)
    Task<(bool Basarili, string? Hata, int ChargeId)> CreateAsync(ManuelBorcCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int tahakkukId, string userId, string neden);

    // Dropdown verileri — DTO döner
    Task<List<LeaseDropdownDto>> GetAktifSozlesmelerAsync();
    Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync();
    Task<List<UnitLookupDto>> GetTumBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null);
}
