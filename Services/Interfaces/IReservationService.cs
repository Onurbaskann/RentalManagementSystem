using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IReservationService
{
    Task<List<RezervasyonListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null);
    Task<RezervasyonListItemDto?> GetByIdAsync(int id);
    Task<RezervasyonHesapSonucu> HesaplaAsync(int unitId, DateTime baslangic, DateTime bitis);
    Task<(bool Basarili, string? Hata, int ReservationId)> CreateAsync(RezervasyonCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden);
    Task<(bool Basarili, string? Hata, int? ChargeId)> TransferToChargeAsync(int id, string userId);

    // Ücret kuralları (birime özel)
    Task<List<RezervasyonTarifeKuralListItemDto>> GetUcretKurallariAsync();
    Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id);
    Task<(bool Basarili, string? Hata, int Id)> SaveUcretKuralAsync(RezervasyonTarifeKuralViewModel model);
    Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id);
}
