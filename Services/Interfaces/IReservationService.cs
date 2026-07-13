using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IReservationService
{
    Task<List<ReservationListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null);
    Task<ReservationListItemDto?> GetByIdAsync(int id);
    Task<RezervasyonHesapSonucu> HesaplaAsync(int unitId, DateTime baslangic, DateTime bitis);
    Task<(bool Basarili, string? Hata, int ReservationId)> CreateAsync(ReservationCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden);
    Task<(bool Basarili, string? Hata, int? ChargeId)> TransferToChargeAsync(int id, string userId);

    // Ücret kuralları (birime özel)
    Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariAsync();
    Task<ReservationRateOverride?> GetUcretKuralByIdAsync(int id);
    Task<(bool Basarili, string? Hata, int Id)> SaveUcretKuralAsync(ReservationRateOverrideViewModel model);
    Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id);
}
