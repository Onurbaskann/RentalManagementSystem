using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PaymentService(
    IPaymentAllocationRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IChargeService chargeService,
    IDocumentService documentService,
    IChargeLineItemRepository chargeLineItemRepository,
    IPaymentStoreResolver storeResolver,
    IPaymentBusinessRules paymentBusinessRules) : IPaymentService, ITransactionalService
{
    public Task<List<PaymentListItemDto>> GetAllAsync(GetPaymentsInput input)
        => paymentRepository.GetListAsync(
            input.ChargeId,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<PagedResult<PaymentListItemDto>> GetPagedAsync(GetPagedPaymentsInput input)
        => paymentRepository.GetPagedListAsync(
            input.Query,
            input.ChargeId,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<PaymentDetailDto?> GetByIdAsync(GetPaymentByIdInput input)
        => paymentRepository.GetDetailsAsync(
            input.PaymentId,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());

    public async Task<ChargeDetailDto> GetCreationContextAsync(GetPaymentCreationContextInput input)
    {
        var charge = Guard.NotFound(
            await chargeService.GetDetailsAsync(new GetChargeDetailsInput(input.ChargeId)),
            "Tahakkuk bulunamadı.",
            "PAYMENT_CHARGE_NOT_FOUND");

        Guard.Forbidden(
            IsOutsideScope(charge, input.AccessScope),
            "Bu tahakkuk için işlem yetkiniz bulunmuyor.",
            "PAYMENT_CHARGE_FORBIDDEN");
        Guard.Conflict(
            charge.Status == ChargeStatus.Cancelled,
            "İptal edilmiş tahakkuka ödeme eklenemez.",
            "PAYMENT_CHARGE_CANCELLED");
        Guard.Conflict(
            charge.Status == ChargeStatus.Paid || charge.TotalAmount - charge.PaidAmount <= 0,
            "Tahakkukun kalan borcu bulunmuyor.",
            "PAYMENT_CHARGE_PAID");

        return charge;
    }

    public Task<List<ChargeLineItemPaymentBalanceDto>> GetPayableLineItemsAsync(int chargeId)
        => GetPayableLineItemBalancesAsync(chargeId);

    public async Task<int> CreateAsync(CreatePaymentInput input)
    {
        var charge = await GetCreationContextAsync(
            new GetPaymentCreationContextInput(input.ChargeId, input.AccessScope));

        var lineItemId = await ResolveTargetLineItemIdAsync(input.ChargeId, input.ChargeLineItemId);
        await chargeLineItemRepository.AcquirePaymentLockAsync(lineItemId);

        var balance = Guard.NotFound(
            await chargeLineItemRepository.GetPaymentBalanceAsync(lineItemId),
            "Tahakkuk kalemi bulunamadı.",
            "PAYMENT_LINE_ITEM_NOT_FOUND");
        paymentBusinessRules.EnsureLineItemBelongsToCharge(balance, input.ChargeId);
        paymentBusinessRules.EnsureLineItemPayable(balance);
        paymentBusinessRules.EnsureAdminAmountWithinAvailable(balance, input.Amount);

        var resolved = await storeResolver.ResolveAsync(balance.ChargeTypeId, balance.UnitId);

        var payment = new PaymentAllocation
        {
            ChargeId = input.ChargeId,
            ChargeLineItemId = balance.ChargeLineItemId,
            StoreAccountId = resolved.StoreAccountId,
            LeaseId = charge.LeaseId,
            PaymentDate = input.PaymentDate,
            Amount = input.Amount,
            PaymentChannel = input.PaymentChannel,
            PaymentSourceType = input.PaymentSourceType,
            PosReferenceNo = input.PosReferenceNo,
            Description = input.Description,
            CreatedByUserId = input.CreatedByUserId,
            EntryDate = DateTime.Now,
            Status = PaymentStatus.PendingApproval
        };

        await paymentRepository.AddAsync(payment);
        await unitOfWork.SaveChangesAsync();

        return payment.Id;
    }

    public async Task ReportTenantPaymentAsync(ReportTenantPaymentInput input)
    {
        var charge = await chargeService.GetTenantDetailsAsync(
            new GetTenantChargeDetailsInput(
                input.ChargeId,
                input.TenantId,
                input.AccessScope.PropertyIds,
                input.AccessScope.UnitIds));
        Guard.Conflict(
            charge.Status == ChargeStatus.Cancelled,
            "İptal edilmiş tahakkuka ödeme eklenemez.",
            "TENANT_PAYMENT_CHARGE_CANCELLED");
        Guard.Conflict(
            charge.Status == ChargeStatus.Paid || charge.TotalAmount - charge.PaidAmount <= 0,
            "Tahakkukun kalan borcu bulunmuyor.",
            "TENANT_PAYMENT_CHARGE_PAID");

        var lineItemId = await ResolveTargetLineItemIdAsync(input.ChargeId, input.ChargeLineItemId);
        await chargeLineItemRepository.AcquirePaymentLockAsync(lineItemId);

        var balance = Guard.NotFound(
            await chargeLineItemRepository.GetPaymentBalanceAsync(lineItemId),
            "Tahakkuk kalemi bulunamadı.",
            "PAYMENT_LINE_ITEM_NOT_FOUND");
        paymentBusinessRules.EnsureLineItemBelongsToCharge(balance, input.ChargeId);
        paymentBusinessRules.EnsureLineItemPayable(balance);
        paymentBusinessRules.EnsureTenantAmountWithinAvailable(balance, input.Amount);

        var resolved = await storeResolver.ResolveAsync(balance.ChargeTypeId, balance.UnitId);

        var receiptType = Guard.NotFound(
            (await documentService.GetTypesAsync(
                new GetDocumentTypesInput(DocumentOwnerType.Payment)))
                .FirstOrDefault(type => type.IsSystem),
            "Ödeme dekontu belge türü bulunamadı.",
            "TENANT_PAYMENT_RECEIPT_TYPE_NOT_FOUND");

        var payment = new PaymentAllocation
        {
            ChargeId = input.ChargeId,
            ChargeLineItemId = balance.ChargeLineItemId,
            StoreAccountId = resolved.StoreAccountId,
            LeaseId = charge.LeaseId,
            PaymentDate = input.PaymentDate,
            Amount = input.Amount,
            PaymentChannel = input.PaymentChannel,
            PaymentSourceType = PaymentSourceType.Manual,
            Description = input.Description,
            CreatedByUserId = input.CreatedByUserId,
            EntryDate = DateTime.Now,
            Status = PaymentStatus.PendingApproval
        };

        await paymentRepository.AddAsync(payment);
        await unitOfWork.SaveChangesAsync();

        await documentService.UploadAsync(new UploadDocumentInput(
            DocumentOwnerType.Payment,
            payment.Id,
            receiptType.Id,
            input.ReceiptFileName,
            input.ReceiptMimeType,
            input.ReceiptContent,
            InvalidateOld: false,
            AccessScope: new DocumentAccessScopeInput(
                [DocumentOwnerType.Payment],
                TenantId: input.TenantId)));
    }

    public async Task ApproveAsync(ApprovePaymentInput input)
    {
        var lineItemId = await paymentRepository.GetChargeLineItemIdAsync(input.PaymentId)
            ?? throw new BusinessException("Ödeme bulunamadı.", ErrorType.NotFound, "PAYMENT_NOT_FOUND");
        await chargeLineItemRepository.AcquirePaymentLockAsync(lineItemId);

        var payment = Guard.NotFound(
            await paymentRepository.GetForDecisionAsync(
                input.PaymentId,
                input.AccessScope.PropertyIds?.ToList(),
                input.AccessScope.UnitIds?.ToList()),
            "Ödeme bulunamadı.",
            "PAYMENT_NOT_FOUND");
        Guard.Conflict(
            payment.Status != PaymentStatus.PendingApproval,
            "Yalnızca onay bekleyen ödeme onaylanabilir.",
            "PAYMENT_NOT_PENDING");

        var balance = Guard.NotFound(
            await chargeLineItemRepository.GetPaymentBalanceAsync(payment.ChargeLineItemId),
            "Tahakkuk kalemi bulunamadı.",
            "PAYMENT_LINE_ITEM_NOT_FOUND");
        paymentBusinessRules.EnsureApprovalWithinRemaining(balance, payment.Amount);

        payment.Status = PaymentStatus.Approved;
        payment.ApprovedByUserId = input.ApprovedByUserId;
        payment.ApprovalDate = DateTime.Now;
        await paymentRepository.UpdateAsync(payment);
        await unitOfWork.SaveChangesAsync();

        await chargeService.UpdatePaidAmountAsync(
            new UpdateChargePaidAmountInput(payment.ChargeId, payment.ChargeLineItemId));
    }

    public async Task RejectAsync(RejectPaymentInput input)
    {
        var lineItemId = await paymentRepository.GetChargeLineItemIdAsync(input.PaymentId)
            ?? throw new BusinessException("Ödeme bulunamadı.", ErrorType.NotFound, "PAYMENT_NOT_FOUND");
        await chargeLineItemRepository.AcquirePaymentLockAsync(lineItemId);

        var payment = Guard.NotFound(
            await paymentRepository.GetForDecisionAsync(
                input.PaymentId,
                input.AccessScope.PropertyIds?.ToList(),
                input.AccessScope.UnitIds?.ToList()),
            "Ödeme bulunamadı.",
            "PAYMENT_NOT_FOUND");
        Guard.Conflict(
            payment.Status != PaymentStatus.PendingApproval,
            "Yalnızca onay bekleyen ödeme reddedilebilir.",
            "PAYMENT_NOT_PENDING");

        payment.Status = PaymentStatus.Rejected;
        payment.RejectionReason = input.Reason;
        await paymentRepository.UpdateAsync(payment);
        await unitOfWork.SaveChangesAsync();

        await chargeService.UpdatePaidAmountAsync(
            new UpdateChargePaidAmountInput(payment.ChargeId, payment.ChargeLineItemId));
    }

    private async Task<int> ResolveTargetLineItemIdAsync(int chargeId, int? explicitLineItemId)
    {
        if (explicitLineItemId.HasValue) return explicitLineItemId.Value;

        var payable = await GetPayableLineItemBalancesAsync(chargeId);
        return paymentBusinessRules.ResolveAutoSelectedLineItem(payable).ChargeLineItemId;
    }

    private async Task<List<ChargeLineItemPaymentBalanceDto>> GetPayableLineItemBalancesAsync(int chargeId)
    {
        var balances = await chargeLineItemRepository.GetPaymentBalancesByChargeAsync(chargeId);
        return balances.Where(balance => balance.AvailableAmount > 0).ToList();
    }

    private static bool IsOutsideScope(
        ChargeDetailDto charge,
        PaymentAccessScopeInput accessScope)
    {
        if (accessScope.PropertyIds == null && accessScope.UnitIds == null)
            return false;

        return charge.PropertyId is not int propertyId
            || charge.UnitId is not int unitId
            || (accessScope.PropertyIds?.Contains(propertyId) != true
                && accessScope.UnitIds?.Contains(unitId) != true);
    }
}
