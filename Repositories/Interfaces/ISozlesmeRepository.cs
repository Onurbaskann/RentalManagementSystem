using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ISozlesmeRepository : IBaseRepository<Lease>
{
    Task<List<SozlesmeListItemDto>> GetListAsync(string? filtre, List<int>? yetkiliTasinmazIds);
    Task<SozlesmeDetayDto?> GetDetayAsync(int id);
    Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId);
    Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds);

    // Dropdown — entity döner (Tenant + Unit + Property yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<SozlesmeDropdownDto>> GetAktifDropdownAsync();

    // RateResolver için projeksiyon: TasinmazId + KiraciKategoriId
    Task<(int TasinmazId, int? KategoriId)?> GetTasinmazVeKategoriAsync(int sozlesmeId);
}
