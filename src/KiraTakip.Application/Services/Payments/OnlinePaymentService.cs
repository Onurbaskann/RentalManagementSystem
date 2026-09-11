using KiraTakip.Data;
using KiraTakip.Domain.Payments;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.OnlinePayment;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Services.Payments;

public class OnlinePaymentService(
    IChargeService chargeService,
    IChargeLineItemRepository chargeLineItemRepository,
    IOnlinePaymentBusinessRules businessRules,
    IOnlinePaymentTransactionRepository transactionRepository,
    IOnlinePaymentEventRepository eventRepository,
    IPaymentStoreResolver storeResolver,
    IStoreAccountRepository storeAccountRepository,
    IStoreAccountCredentialProtector credentialProtector,
    IEnumerable<IOnlinePaymentProvider> providers,
    IUnitOfWork unitOfWork) : IOnlinePaymentService, ITransactionalService
{
    public async Task<InitiateOnlinePaymentResult> InitiateAsync(
        InitiateOnlinePaymentInput input,
        CancellationToken cancellationToken = default)
    {
        var charge = await chargeService.GetTenantDetailsAsync(
            new GetTenantChargeDetailsInput(
                input.ChargeId,
                input.TenantId,
                input.AccessScope.PropertyIds,
                input.AccessScope.UnitIds));
        var eligibility = PaymentChargeEligibilityPolicy.CheckEligibility(
            charge.Status,
            charge.TotalAmount,
            charge.PaidAmount);

        Guard.Conflict(
            eligibility == ChargePaymentEligibility.Cancelled,
            "İptal edilmiş tahakkuka ödeme eklenemez.",
            "ONLINE_PAYMENT_CHARGE_CANCELLED");
        Guard.Conflict(
            eligibility == ChargePaymentEligibility.NoRemainingBalance,
            "Tahakkukun kalan borcu bulunmuyor.",
            "ONLINE_PAYMENT_CHARGE_PAID");

        await chargeLineItemRepository.AcquirePaymentLockAsync(input.ChargeLineItemId);

        var balance = Guard.NotFound(
            await chargeLineItemRepository.GetPaymentBalanceAsync(input.ChargeLineItemId, cancellationToken),
            "Tahakkuk kalemi bulunamadı.",
            "ONLINE_PAYMENT_LINE_ITEM_NOT_FOUND");
        Guard.Conflict(
            !PaymentAmountPolicy.BelongsToCharge(balance.ChargeId, input.ChargeId),
            "Seçilen kalem bu tahakkuka ait değil.",
            "ONLINE_PAYMENT_LINE_ITEM_CHARGE_MISMATCH");
        businessRules.EnsureAmountWithinAvailable(balance, input.Amount);

        Guard.Conflict(
            await transactionRepository.HasActiveAttemptAsync(input.ChargeLineItemId, cancellationToken),
            "Bu kalem için sonuçlanmamış bir sanal POS denemesi zaten var.",
            "ONLINE_PAYMENT_ACTIVE_ATTEMPT_EXISTS");

        var resolved = await storeResolver.ResolveAsync(balance.ChargeTypeId, balance.UnitId, cancellationToken);

        var provider = Guard.NotFound(
            providers.FirstOrDefault(candidate => candidate.ProviderCode == resolved.ProviderCode),
            $"'{resolved.ProviderCode}' için sanal POS sağlayıcısı bulunamadı.",
            "ONLINE_PAYMENT_PROVIDER_NOT_FOUND");

        var storeAccount = Guard.NotFound(
            await storeAccountRepository.GetByIdAsync(resolved.StoreAccountId),
            "Mağaza hesabı bulunamadı.",
            "ONLINE_PAYMENT_STORE_ACCOUNT_NOT_FOUND");

        var account = new PaymentProviderAccount(
            storeAccount.ProviderCode,
            storeAccount.MerchantId,
            storeAccount.MerchantUser,
            credentialProtector.Unprotect(storeAccount.ProtectedMerchantPassword),
            storeAccount.Currency);

        var merchantPaymentId = Guid.NewGuid().ToString("N");

        var sessionResult = await provider.CreateSessionAsync(
            new CreatePaymentSessionRequest(merchantPaymentId, input.Amount, storeAccount.Currency),
            account,
            cancellationToken);

        var transaction = new OnlinePaymentTransaction
        {
            ChargeLineItemId = input.ChargeLineItemId,
            StoreAccountId = resolved.StoreAccountId,
            InitiatedByUserId = input.InitiatedByUserId,
            ProviderCode = resolved.ProviderCode,
            MerchantPaymentId = merchantPaymentId,
            ProviderTransactionId = sessionResult.ProviderTransactionId,
            Amount = input.Amount,
            Currency = storeAccount.Currency,
            Status = OnlinePaymentStatusTransitionPolicy.ResolveInitialStatus(sessionResult.IsSuccessful),
            ResponseCode = sessionResult.ResponseCode,
            TransactionStatus = sessionResult.TransactionStatus,
            ErrorCode = sessionResult.ErrorCode,
            SafeMessage = sessionResult.SafeMessage,
            SessionExpiresAt = sessionResult.SessionExpiresAt
        };

        await transactionRepository.AddAsync(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventRepository.AddAsync(new OnlinePaymentEvent
        {
            OnlinePaymentTransactionId = transaction.Id,
            EventType = OnlinePaymentEventType.SessionRequested,
            SafeSummary = $"Oturum isteği gönderildi. Tutar: {input.Amount:N2} {storeAccount.Currency}."
        }, cancellationToken);
        await eventRepository.AddAsync(new OnlinePaymentEvent
        {
            OnlinePaymentTransactionId = transaction.Id,
            EventType = OnlinePaymentEventType.SessionResult,
            ProviderResponseCode = sessionResult.ResponseCode,
            ProviderTransactionStatus = sessionResult.TransactionStatus,
            SafeSummary = sessionResult.SafeMessage
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InitiateOnlinePaymentResult(
            transaction.Id,
            transaction.ProviderCode,
            transaction.MerchantPaymentId,
            sessionResult.SessionToken,
            transaction.SessionExpiresAt,
            transaction.Status);
    }
}
