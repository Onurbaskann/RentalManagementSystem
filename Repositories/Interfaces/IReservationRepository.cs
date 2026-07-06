using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRepository : IBaseRepository<Reservation>
{
    // Listeleme (DTO)
    Task<List<RezervasyonListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds);
    Task<RezervasyonListItemDto?> GetByIdAsync(int id);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(int unitId, DateTime baslangic, DateTime bitis);

    // RezervasyonTarife — Reservation domain
    Task<RezervasyonTarife?> GetAktifTarifeForBirimAsync(int unitId);
    Task<RezervasyonTarife?> GetGenelTarifeAsync(int unitTypeId, int yil);
    Task<List<RezervasyonTarife>> GetUcretKurallariAsync();
    Task<RezervasyonTarife?> GetUcretKuralByIdAsync(int id);
    Task AddUcretKuralAsync(RezervasyonTarife kural);

    // ChargeType — rezervasyon tahakkuk üretimi için
    Task<ChargeType?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId);

    // Charge — Transfer işlemi için
    Task AddTahakkukAsync(Charge tahakkuk);
}
