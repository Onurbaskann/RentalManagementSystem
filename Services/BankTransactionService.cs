using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Banka;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class BankTransactionService(
    IBankTransactionRepository bankTransactionRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    IPaymentMatchRepository paymentMatchRepository,
    IEnumerable<IBankaHareketiParser> parsers,
    IOperationalPolicyProvider operationalPolicyProvider,
    IStoreRepository storeRepository,
    IStoreAccountRepository storeAccountRepository,
    IUnitOfWork unitOfWork) : IBankTransactionService, ITransactionalService
{
    public async Task ImportAsync(ImportBankTransactionsInput input)
    {
        var parser = parsers.FirstOrDefault(item =>
            item.BankCode.Equals(input.BankCode, StringComparison.OrdinalIgnoreCase));
        Guard.InvalidField(
            parser == null,
            nameof(input.BankCode),
            $"'{input.BankCode}' için parser bulunamadı.");

        var account = Guard.NotFound(
            await storeAccountRepository.GetActiveByStoreIdAsync(input.StoreId, tracking: false),
            "Seçilen mağazanın aktif ödeme hesabı bulunamadı.",
            "BANK_IMPORT_STORE_ACCOUNT_NOT_FOUND");

        var transactions = parser!.Parse(input.File).ToList();
        Guard.InvalidField(
            transactions.Count == 0,
            nameof(input.File),
            "Dosyada içe aktarılabilir banka hareketi bulunamadı.",
            "BANK_IMPORT_NO_TRANSACTIONS");

        foreach (var transaction in transactions)
            transaction.StoreAccountId = account.Id;

        await bankTransactionRepository.AddRangeAsync(transactions);
        await unitOfWork.SaveChangesAsync();
    }

    public Task<List<StoreRoutingOptionDto>> GetImportStoreOptionsAsync()
        => storeRepository.GetRoutingOptionsAsync();

    public Task<List<BankTransactionListItemDto>> GetAllAsync(GetBankTransactionsInput input)
        => bankTransactionRepository.GetListAsync(input.Status);

    public Task<PagedResult<BankTransactionListItemDto>> GetPagedAsync(TableQuery query)
        => bankTransactionRepository.GetPagedListAsync(query);

    public Task<BankTransactionDetailDto?> GetByIdAsync(GetBankTransactionByIdInput input)
        => bankTransactionRepository.GetDetailAsync(input.Id);

    public async Task MatchAsync(MatchBankTransactionInput input)
    {
        var payment = Guard.NotFound(
            await paymentAllocationRepository.GetMatchingContextAsync(input.PaymentId),
            "Ödeme bulunamadı.");

        Guard.Forbidden(
            IsOutsideScope(
                payment.PropertyId,
                payment.UnitId,
                input.PropertyIds,
                input.UnitIds),
            "Bu ödeme için banka eşleştirmesi yapma yetkiniz bulunmuyor.");

        Guard.Conflict(
            payment.Status is not PaymentStatus.PendingApproval and not PaymentStatus.Approved,
            "Yalnızca onay bekleyen veya onaylanmış ödemeler banka hareketiyle eşleştirilebilir.");

        Guard.Conflict(
            await paymentMatchRepository.ExistsForPaymentAsync(input.PaymentId),
            "Ödeme başka bir banka hareketiyle zaten eşleştirilmiş.");

        var transaction = Guard.NotFound(
            await bankTransactionRepository.GetByIdAsync(input.BankTransactionId),
            "Banka hareketi bulunamadı.");

        Guard.Conflict(
            transaction.MatchStatus != BankMatchStatus.Unmatched
                || await paymentMatchRepository.ExistsForBankTransactionAsync(input.BankTransactionId),
            "Banka hareketi başka bir ödemeyle zaten eşleştirilmiş.");

        Guard.Conflict(
            transaction.StoreAccountId != payment.StoreAccountId,
            "Banka hareketi ile ödeme farklı mağaza hesaplarına ait olduğu için eşleştirilemez.",
            "BANK_MATCH_STORE_ACCOUNT_MISMATCH");

        var match = new PaymentMatch
        {
            PaymentAllocationId = input.PaymentId,
            BankTransactionId = input.BankTransactionId,
            MatchType = Models.MatchType.Manual,
        };

        transaction.MatchStatus = BankMatchStatus.ManuallyMatched;

        await paymentMatchRepository.AddAsync(match);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<int> UnmatchAsync(UnmatchBankTransactionInput input)
    {
        var match = Guard.NotFound(
            await paymentMatchRepository.GetWithDetailsAsync(input.MatchId),
            "Banka eşleşmesi bulunamadı.");

        Guard.Forbidden(
            IsOutsideScope(
                match.PaymentAllocation.Charge.Unit.PropertyId,
                match.PaymentAllocation.Charge.UnitId,
                input.PropertyIds,
                input.UnitIds),
            "Bu banka eşleşmesini kaldırma yetkiniz bulunmuyor.");

        var paymentId = match.PaymentAllocationId;

        await paymentMatchRepository.RemoveAsync(match);
        match.BankTransaction.MatchStatus = BankMatchStatus.Unmatched;

        await unitOfWork.SaveChangesAsync();

        return paymentId;
    }

    public async Task<List<PaymentCandidateDto>> GetPaymentCandidatesAsync(GetBankTransactionPaymentCandidatesInput input)
    {
        if (await paymentMatchRepository.ExistsForBankTransactionAsync(input.BankTransactionId))
            return [];

        var basis = await bankTransactionRepository.GetMatchingBasisAsync(input.BankTransactionId);
        return basis == null
            ? []
            : await paymentAllocationRepository.GetCandidatesAsync(
                basis,
                GetMatchingPolicy(),
                input.PropertyIds,
                input.UnitIds);
    }

    public async Task<List<BankTransactionListItemDto>> GetTransactionCandidatesAsync(GetBankTransactionCandidatesInput input)
    {
        if (await paymentMatchRepository.ExistsForPaymentAsync(input.PaymentId))
            return [];

        var basis = await paymentAllocationRepository.GetMatchingBasisAsync(input.PaymentId);
        return basis == null
            ? []
            : await bankTransactionRepository.GetTransactionCandidatesAsync(
                basis,
                GetMatchingPolicy());
    }

    private static bool IsOutsideScope(
        int propertyId,
        int unitId,
        IReadOnlyCollection<int>? propertyIds,
        IReadOnlyCollection<int>? unitIds)
    {
        if (propertyIds == null && unitIds == null)
            return false;

        return propertyIds?.Contains(propertyId) != true
            && unitIds?.Contains(unitId) != true;
    }

    private PaymentMatchingPolicyDto GetMatchingPolicy()
    {
        var policy = operationalPolicyProvider.Current;
        return new PaymentMatchingPolicyDto(
            policy.BankMatchingAmountTolerancePercent,
            policy.BankMatchingDateToleranceDays);
    }
}
