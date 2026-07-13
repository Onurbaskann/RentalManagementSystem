using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRepository : IBaseRepository<Reservation>
{
    // Listeleme (DTO)
    Task<List<ReservationListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds);
    Task<ReservationListItemDto?> GetByIdAsync(int id);

    // Çakışma kontrolü
    Task<bool> IsConflictAsync(int unitId, DateTime baslangic, DateTime bitis);

    // ReservationRateOverride — Reservation domain
    Task<ReservationRateOverride?> GetAktifTarifeForBirimAsync(int unitId);
    Task<ReservationRateOverride?> GetGenelTarifeAsync(int unitTypeId, int yil);
    Task<List<ReservationRateOverride>> GetUcretKurallariAsync();
    Task<ReservationRateOverride?> GetUcretKuralByIdAsync(int id);
    Task AddUcretKuralAsync(ReservationRateOverride kural);

    // ChargeType — reservation charge üretimi için
    Task<ChargeType?> ResolveRezervasyonBorcTipiAsync(int? preferredBorcTipiId);

    // Charge — Transfer işlemi için
    Task AddTahakkukAsync(Charge charge);
}
