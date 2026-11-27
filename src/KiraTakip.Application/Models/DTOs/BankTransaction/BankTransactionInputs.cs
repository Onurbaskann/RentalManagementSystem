using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.BankTransaction;

public record ImportBankTransactionsInput(Stream File, string BankCode, int StoreId);

public record GetBankTransactionsInput(BankMatchStatus? Status = null);

public record GetBankTransactionByIdInput(int Id);

public record MatchBankTransactionInput(
    int PaymentId,
    int BankTransactionId,
    SpaceAccessScope? Scope = null);

public record UnmatchBankTransactionInput(
    int MatchId,
    SpaceAccessScope? Scope = null);

public record GetBankTransactionPaymentCandidatesInput(
    int BankTransactionId,
    SpaceAccessScope? Scope = null);

public record GetBankTransactionCandidatesInput(int PaymentId);

public record PaymentMatchingBasisDto(decimal Amount, DateTime Date, int StoreAccountId);

public record PaymentMatchingPolicyDto(
    decimal AmountTolerancePercent,
    int DateToleranceDays);

public record PaymentMatchingContextDto(
    int PaymentId,
    PaymentStatus Status,
    int StoreAccountId);
