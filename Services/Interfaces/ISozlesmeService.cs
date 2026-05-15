using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface ISozlesmeService
{
    Task<List<KiraSozlesmesi>> GetAllAsync(string? filtre = null, string? userId = null);
    Task<KiraSozlesmesi?> GetByIdAsync(int id);
    Task<KiraSozlesmesi> CreateAsync(KiraSozlesmesi s, decimal? aylikBedel = null);
    Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel, bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama);
    Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama);
    Task<List<KiraSozlesmesi>> GetByKiraciIdAsync(int kiraciId);
    Task<List<KiraSozlesmesi>> GetByBirimIdAsync(int birimId);
}
