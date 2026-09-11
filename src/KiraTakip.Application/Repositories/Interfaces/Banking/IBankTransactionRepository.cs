using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.BankTransaction;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Banking;

public interface IBankTransactionRepository : IRepositoryBase<BankTransaction>
{
    // Listeleme (DTO)
    Task<List<BankTransactionListItemDto>> GetListAsync(BankMatchStatus? status = null);
    Task<PagedResult<BankTransactionListItemDto>> GetPagedListAsync(TableQuery query);
    Task<BankTransactionDetailDto?> GetDetailAsync(int id);

    // Eşleştirme adayları (DTO)
    Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int bankTransactionId);
    Task<List<BankTransactionListItemDto>> GetTransactionCandidatesAsync(
        PaymentMatchingBasisDto basis,
        PaymentMatchingPolicyDto policy);

    // Eşleştirme yazma işlemleri
    // CSV import için toplu ekleme
    Task AddRangeAsync(IEnumerable<BankTransaction> entities);
}
