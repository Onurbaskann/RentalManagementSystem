using KiraTakip.Models.Common;
using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Payment;

public record GetPaymentsInput(
    int? ChargeId = null,
    SpaceAccessScope? Scope = null);

public record GetPagedPaymentsInput(
    TableQuery Query,
    int? ChargeId = null,
    SpaceAccessScope? Scope = null);

public record GetPaymentByIdInput(
    int PaymentId,
    SpaceAccessScope AccessScope);

public record GetPaymentCreationContextInput(
    int ChargeId,
    SpaceAccessScope AccessScope);

public record CreatePaymentInput(
    int ChargeId,
    DateTime PaymentDate,
    decimal Amount,
    PaymentChannel PaymentChannel,
    PaymentSourceType PaymentSourceType,
    string? Description,
    string CreatedByUserId,
    SpaceAccessScope AccessScope,
    int? ChargeLineItemId,
    string? PosReferenceNo = null);

public record ReportTenantPaymentInput(
    int TenantId,
    int ChargeId,
    DateTime PaymentDate,
    decimal Amount,
    PaymentChannel PaymentChannel,
    string? Description,
    string CreatedByUserId,
    string ReceiptFileName,
    string ReceiptMimeType,
    byte[] ReceiptContent,
    SpaceAccessScope AccessScope,
    int? ChargeLineItemId);

public record ApprovePaymentInput(
    int PaymentId,
    string ApprovedByUserId,
    SpaceAccessScope AccessScope);

public record RejectPaymentInput(
    int PaymentId,
    string Reason,
    SpaceAccessScope AccessScope);
