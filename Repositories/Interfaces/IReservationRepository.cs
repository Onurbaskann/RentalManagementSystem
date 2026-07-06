using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRepository : IBaseRepository<Reservation>
{
    // Listeleme (DTO)
    Task<List<RezervasyonListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds);
    Task<RezervasyonListItemDto?> GetByIdAsync(int id);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(int birimId, DateTime baslangic, DateTime bitis);

    // RezervasyonTarife — Reservation domain
    Task<RezervasyonTarife?> GetAktifTarifeForBirimAsync(int birimId);
    Task<RezervasyonTarife?> GetGenelTarifeAsync(int birimTuruId, int yil);
    Task<List<RezervasyonTarife>> GetUcretKurallariAsync();
    Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id);
    Task AddUcretKuralAsync(RezervasyonTarife kural);

    // ChargeType — rezervasyon tahakkuk üretimi için
    Task<ChargeType?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId);

    // Charge — Transfer işlemi için
    Task AddTahakkukAsync(Charge tahakkuk);
}
