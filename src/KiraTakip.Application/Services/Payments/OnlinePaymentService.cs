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
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Services.Payments;

public class OnlinePaymentService(
    IChargeService chargeService,
    IChargeLineItemRepository chargeLineItemRepository,
    IOnlinePaymentBusinessRules businessRules,
    IOnlinePaymentTransactionRepository transactionRepository,
    IOnlinePaymentEventRepository eventRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    IPaymentStoreResolver storeResolver,
    IStoreAccountRepository storeAccountRepository,
    IStoreAccountCredentialProtector credentialProtector,
    ITenantRepository tenantRepository,
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

        var tenant = Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId, cancellationToken),
            "Kiracı bulunamadı.",
            "ONLINE_PAYMENT_TENANT_NOT_FOUND");

        var merchantPaymentId = Guid.NewGuid().ToString("N");

        var sessionResult = await provider.CreateSessionAsync(
            new CreatePaymentSessionRequest(
                merchantPaymentId,
                input.Amount,
                storeAccount.Currency,
                tenant.TenantNo,
                tenant.Name,
                tenant.Email,
                tenant.Phone),
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
            transaction.Status,
            sessionResult.IsSuccessful && sessionResult.SessionToken is not null
                ? provider.BuildHostedPaymentPageUrl(sessionResult.SessionToken)
                : null);
    }

    public async Task<CompleteOnlinePaymentResult> CompleteAsync(
        CompleteOnlinePaymentInput input,
        CancellationToken cancellationToken = default)
    {
        var transaction = Guard.NotFound(
            await transactionRepository.GetByMerchantPaymentIdAsync(input.MerchantPaymentId, cancellationToken),
            "Sanal POS işlemi bulunamadı.",
            "ONLINE_PAYMENT_TRANSACTION_NOT_FOUND");

        Guard.Forbidden(
            transaction.ChargeLineItem.Charge.TenantId != input.TenantId,
            "Bu işlem için erişim yetkiniz bulunmuyor.",
            "ONLINE_PAYMENT_TRANSACTION_FORBIDDEN");

        await chargeLineItemRepository.AcquirePaymentLockAsync(transaction.ChargeLineItemId);

        var provider = Guard.NotFound(
            providers.FirstOrDefault(candidate => candidate.ProviderCode == transaction.ProviderCode),
            $"'{transaction.ProviderCode}' için sanal POS sağlayıcısı bulunamadı.",
            "ONLINE_PAYMENT_PROVIDER_NOT_FOUND");

        var storeAccount = Guard.NotFound(
            await storeAccountRepository.GetByIdAsync(transaction.StoreAccountId),
            "Mağaza hesabı bulunamadı.",
            "ONLINE_PAYMENT_STORE_ACCOUNT_NOT_FOUND");

        var account = new PaymentProviderAccount(
            storeAccount.ProviderCode,
            storeAccount.MerchantId,
            storeAccount.MerchantUser,
            credentialProtector.Unprotect(storeAccount.ProtectedMerchantPassword),
            storeAccount.Currency);

        var inquiryResult = await provider.QueryAsync(
            new PaymentInquiryRequest(transaction.MerchantPaymentId),
            account,
            cancellationToken);

        var newStatus = businessRules.NormalizeProviderStatus(
            inquiryResult.ResponseCode,
            inquiryResult.TransactionStatus);

        transaction.LastInquiryAt = DateTime.UtcNow;
        transaction.InquiryCount += 1;
        transaction.ResponseCode = inquiryResult.ResponseCode;
        transaction.TransactionStatus = inquiryResult.TransactionStatus;
        if (!string.IsNullOrWhiteSpace(inquiryResult.ProviderTransactionId))
            transaction.ProviderTransactionId = inquiryResult.ProviderTransactionId;

        await eventRepository.AddAsync(new OnlinePaymentEvent
        {
            OnlinePaymentTransactionId = transaction.Id,
            EventType = OnlinePaymentEventType.InquiryPerformed,
            ProviderResponseCode = inquiryResult.ResponseCode,
            ProviderTransactionStatus = inquiryResult.TransactionStatus,
            SafeSummary = inquiryResult.SafeMessage
        }, cancellationToken);

        // Geçersiz geçiş (örn. zaten terminal durumda, sayfa yenilendi) sessizce yok sayılır —
        // hata fırlatılmaz, yalnız sorgulama izi kaydedilir (İç Faz 7 planı, idempotent no-op).
        if (businessRules.IsValidStatusTransition(transaction.Status, newStatus))
        {
            transaction.Status = newStatus;

            if (newStatus == OnlinePaymentTransactionStatus.Approved)
            {
                var charge = transaction.ChargeLineItem.Charge;
                var payment = new PaymentAllocation
                {
                    ChargeId = charge.Id,
                    ChargeLineItemId = transaction.ChargeLineItemId,
                    StoreAccountId = transaction.StoreAccountId,
                    LeaseId = charge.LeaseId,
                    CreatedByUserId = transaction.InitiatedByUserId,
                    PaymentDate = DateTime.Today,
                    Amount = transaction.Amount,
                    PaymentChannel = PaymentChannel.Card,
                    PaymentSourceType = PaymentSourceType.VirtualPos,
                    PosReferenceNo = transaction.ProviderTransactionId,
                    Status = PaymentStatus.Approved,
                    EntryDate = DateTime.Now,
                    ApprovalDate = DateTime.Now
                };

                await paymentAllocationRepository.AddAsync(payment);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                transaction.PaymentAllocationId = payment.Id;
                transaction.CompletedAt = DateTime.UtcNow;

                await eventRepository.AddAsync(new OnlinePaymentEvent
                {
                    OnlinePaymentTransactionId = transaction.Id,
                    EventType = OnlinePaymentEventType.Succeeded,
                    ProviderResponseCode = inquiryResult.ResponseCode,
                    ProviderTransactionStatus = inquiryResult.TransactionStatus,
                    SafeSummary = "Ödeme onaylandı."
                }, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await chargeService.UpdatePaidAmountAsync(
                    new UpdateChargePaidAmountInput(charge.Id, transaction.ChargeLineItemId));
            }
            else if (newStatus is OnlinePaymentTransactionStatus.Failed or OnlinePaymentTransactionStatus.Cancelled)
            {
                transaction.CompletedAt = DateTime.UtcNow;

                await eventRepository.AddAsync(new OnlinePaymentEvent
                {
                    OnlinePaymentTransactionId = transaction.Id,
                    EventType = OnlinePaymentEventType.Failed,
                    ProviderResponseCode = inquiryResult.ResponseCode,
                    ProviderTransactionStatus = inquiryResult.TransactionStatus,
                    SafeSummary = inquiryResult.SafeMessage
                }, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CompleteOnlinePaymentResult(
            transaction.Status,
            transaction.PaymentAllocationId,
            transaction.ChargeLineItem.ChargeId);
    }
}
