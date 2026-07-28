namespace KiraTakip.Models.Dtos;

public record ImportBankTransactionsInput(Stream File, string BankCode);

public record GetBankTransactionsInput(BankMatchStatus? Status = null);

public record GetBankTransactionByIdInput(int Id);

public record MatchBankTransactionInput(
    int PaymentId,
    int BankTransactionId,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record UnmatchBankTransactionInput(
    int MatchId,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetBankTransactionPaymentCandidatesInput(
    int BankTransactionId,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetBankTransactionCandidatesInput(int PaymentId);

public record PaymentMatchingBasisDto(decimal Amount, DateTime Date);

public record PaymentMatchingContextDto(
    int PaymentId,
    int PropertyId,
    int UnitId,
    PaymentStatus Status);
