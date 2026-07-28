using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IBankTransactionService
{
    Task ImportAsync(ImportBankTransactionsInput input);
    Task<List<BankTransactionListItemDto>> GetAllAsync(GetBankTransactionsInput input);
    Task<PagedResult<BankTransactionListItemDto>> GetPagedAsync(TableQuery query);
    Task<BankTransactionDetailDto?> GetByIdAsync(GetBankTransactionByIdInput input);
    Task MatchAsync(MatchBankTransactionInput input);
    Task<int> UnmatchAsync(UnmatchBankTransactionInput input);
    Task<List<PaymentCandidateDto>> GetPaymentCandidatesAsync(GetBankTransactionPaymentCandidatesInput input);
    Task<List<BankTransactionListItemDto>> GetTransactionCandidatesAsync(GetBankTransactionCandidatesInput input);
}
