using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ISozlesmeService
{
    Task<List<SozlesmeListItemDto>> GetAllAsync(string? filtre = null, string? userId = null);
    Task<SozlesmeDetayDto?> GetByIdAsync(int id);
    Task<KiraSozlesmesi> CreateAsync(KiraSozlesmesi s, decimal? aylikBedel = null);
    Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel, bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama);
    Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama);
    Task VadeGuncelleAsync(int id, VadeKuraliTipi tip, int gun, string? aciklama);
    Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId);
    Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds);
}
