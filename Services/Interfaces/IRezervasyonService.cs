using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IRezervasyonService
{
    Task<List<Rezervasyon>> GetAllAsync(string? userId = null);
    Task<Rezervasyon?> GetByIdAsync(int id);
    Task<RezervasyonHesapSonucu> HesaplaAsync(int birimId, DateTime baslangic, DateTime bitis);
    Task<(bool Basarili, string? Hata, int RezervasyonId)> CreateAsync(RezervasyonCreateViewModel model, string userId);
    Task<(bool Basarili, string? Hata)> CancelAsync(int id, string userId, string neden);
    Task<(bool Basarili, string? Hata, int? TahakkukId)> TransferToTahakkukAsync(int id, string userId);

    // Ücret kuralları (birime özel)
    Task<List<RezervasyonTarife>> GetUcretKurallariAsync();
    Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id);
    Task<(bool Basarili, string? Hata, int Id)> SaveUcretKuralAsync(RezervasyonTarifeKuralViewModel model);
    Task<(bool Basarili, string? Hata)> ToggleUcretKuralAktifAsync(int id);
}
