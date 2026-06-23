using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IRezervasyonRepository : IBaseRepository<Rezervasyon>
{
    // Listeleme (DTO)
    Task<List<RezervasyonListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(int birimId, DateTime baslangic, DateTime bitis);

    // RezervasyonTarife — Rezervasyon domain
    Task<RezervasyonTarife?> GetAktifTarifeForBirimAsync(int birimId);
    Task<RezervasyonTarife?> GetGenelTarifeAsync(int birimTuruId, int yil);
    Task<List<RezervasyonTarife>> GetUcretKurallariAsync();
    Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id);
    Task AddUcretKuralAsync(RezervasyonTarife kural);

    // BorcTipi — rezervasyon tahakkuk üretimi için
    Task<BorcTipi?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId);

    // Tahakkuk — Transfer işlemi için
    Task AddTahakkukAsync(Tahakkuk tahakkuk);
}
