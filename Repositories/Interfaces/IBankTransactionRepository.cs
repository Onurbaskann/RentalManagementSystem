using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBankTransactionRepository : IBaseRepository<BankTransaction>
{
    // Listeleme (DTO)
    Task<List<BankaHareketiListItemDto>> GetListAsync(BankMatchStatus? durum = null);
    Task<PagedResult<BankaHareketiListItemDto>> GetPagedListAsync(TableQuery q);
    Task<BankaHareketiDetayDto?> GetDetayAsync(int id);

    // Eşleştirme adayları (DTO)
    Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, IReadOnlyList<int>? tasinmazIds = null);
    Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId);

    // Eşleştirme yazma işlemleri
    Task<bool> EslesmeVarMiAsync(int tahakkukOdemeId, int bankaHareketiId);
    Task AddEslesmeAsync(PaymentMatch eslesme);
    Task<PaymentMatch?> GetEslesmeWithBankaHareketiAsync(int eslesmeId);
    Task RemoveEslesmeAsync(PaymentMatch eslesme);
    Task<bool> KalanEslesmeVarMiAsync(int bankaHareketiId, int excludeEslesmeId);

    // CSV import için toplu ekleme
    Task AddRangeAsync(IEnumerable<BankTransaction> entities);
}
