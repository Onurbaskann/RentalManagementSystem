using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBankTransactionRepository : IRepositoryBase<BankTransaction>
{
    // Listeleme (DTO)
    Task<List<BankTransactionListItemDto>> GetListAsync(BankMatchStatus? status = null);
    Task<PagedResult<BankTransactionListItemDto>> GetPagedListAsync(TableQuery query);
    Task<BankTransactionDetailDto?> GetDetailAsync(int id);

    // Eşleştirme adayları (DTO)
    Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int bankTransactionId);
    Task<List<BankTransactionListItemDto>> GetTransactionCandidatesAsync(PaymentMatchingBasisDto basis);

    // Eşleştirme yazma işlemleri
    // CSV import için toplu ekleme
    Task AddRangeAsync(IEnumerable<BankTransaction> entities);
}
