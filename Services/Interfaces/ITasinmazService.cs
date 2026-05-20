using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface ITasinmazService
{
    Task<List<Tasinmaz>> GetAllAsync(string? userId = null);
    Task<Tasinmaz?> GetByIdAsync(int id);
    Task<Tasinmaz> CreateAsync(Tasinmaz t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null);
    Task UpdateAsync(Tasinmaz t);
    Task<List<Birim>> GetBosBirimlerAsync(string? userId = null);
}
