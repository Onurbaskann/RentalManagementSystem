using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBankaHareketiRepository : IBaseRepository<BankaHareketi>
{
    // Listeleme (DTO)
    Task<List<BankaHareketiListItemDto>> GetListAsync(BankaEslesmeDurumu? durum = null);
    Task<PagedResult<BankaHareketiListItemDto>> GetPagedListAsync(TableQuery q);
    Task<BankaHareketiDetayDto?> GetDetayAsync(int id);

    // Eşleştirme adayları (DTO)
    Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, string? userId = null);
    Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId);

    // Eşleştirme yazma işlemleri
    Task<bool> EslesmeVarMiAsync(int kiraOdemeId, int bankaHareketiId);
    Task AddEslesmeAsync(OdemeBankaEslesme eslesme);
    Task<OdemeBankaEslesme?> GetEslesmeWithBankaHareketiAsync(int eslesmeId);
    Task RemoveEslesmeAsync(OdemeBankaEslesme eslesme);
    Task<bool> KalanEslesmeVarMiAsync(int bankaHareketiId, int excludeEslesmeId);

    // CSV import için toplu ekleme
    Task AddRangeAsync(IEnumerable<BankaHareketi> entities);
}
