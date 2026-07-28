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
    IDocumentService documentService) : IPaymentService, ITransactionalService
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

    public async Task<int> CreateAsync(CreatePaymentInput input)
    {
        var charge = await GetCreationContextAsync(
            new GetPaymentCreationContextInput(input.ChargeId, input.AccessScope));
        var remainingAmount = charge.TotalAmount - charge.PaidAmount;
        Guard.InvalidField(
            input.Amount > remainingAmount,
            nameof(input.Amount),
            $"Tutar kalan borçtan ({remainingAmount:N2} ₺) küçük veya eşit olmalıdır.",
            "PAYMENT_AMOUNT_EXCEEDS_REMAINING");

        var payment = new PaymentAllocation
        {
            ChargeId = input.ChargeId,
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

        var pendingAmount = await paymentRepository.GetPendingAmountAsync(
            input.ChargeId,
            input.TenantId);
        var availableAmount = charge.TotalAmount - charge.PaidAmount - pendingAmount;
        Guard.Against(
            input.Amount > availableAmount,
            $"Tutar 0'dan büyük ve kalan borçtan ({Math.Max(0, availableAmount):N2} ₺) küçük/eşit olmalıdır.",
            "TENANT_PAYMENT_AMOUNT_EXCEEDS_AVAILABLE");

        var receiptType = Guard.NotFound(
            (await documentService.GetTypesAsync(
                new GetDocumentTypesInput(DocumentOwnerType.Payment)))
                .FirstOrDefault(type => type.IsSystem),
            "Ödeme dekontu belge türü bulunamadı.",
            "TENANT_PAYMENT_RECEIPT_TYPE_NOT_FOUND");

        var payment = new PaymentAllocation
        {
            ChargeId = input.ChargeId,
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

        var approvedAmount = await paymentRepository.GetPaidAmountAsync(payment.ChargeId);
        Guard.Conflict(
            approvedAmount + payment.Amount > payment.Charge.TotalAmount,
            "Ödeme tutarı tahakkukun kalan borcunu aşıyor.",
            "PAYMENT_APPROVAL_EXCEEDS_REMAINING");

        payment.Status = PaymentStatus.Approved;
        payment.ApprovedByUserId = input.ApprovedByUserId;
        payment.ApprovalDate = DateTime.Now;
        await paymentRepository.UpdateAsync(payment);
        await unitOfWork.SaveChangesAsync();

        await chargeService.UpdatePaidAmountAsync(new UpdateChargePaidAmountInput(payment.ChargeId));
    }

    public async Task RejectAsync(RejectPaymentInput input)
    {
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

        await chargeService.UpdatePaidAmountAsync(new UpdateChargePaidAmountInput(payment.ChargeId));
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
