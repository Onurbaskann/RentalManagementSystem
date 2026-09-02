using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetPaymentsInput(
    int? ChargeId = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPagedPaymentsInput(
    TableQuery Query,
    int? ChargeId = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record PaymentAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPaymentByIdInput(
    int PaymentId,
    PaymentAccessScopeInput AccessScope);

public record GetPaymentCreationContextInput(
    int ChargeId,
    PaymentAccessScopeInput AccessScope);

public record CreatePaymentInput(
    int ChargeId,
    DateTime PaymentDate,
    decimal Amount,
    PaymentChannel PaymentChannel,
    PaymentSourceType PaymentSourceType,
    string? Description,
    string CreatedByUserId,
    PaymentAccessScopeInput AccessScope,
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
    PaymentAccessScopeInput AccessScope,
    int? ChargeLineItemId);

public record ApprovePaymentInput(
    int PaymentId,
    string ApprovedByUserId,
    PaymentAccessScopeInput AccessScope);

public record RejectPaymentInput(
    int PaymentId,
    string Reason,
    PaymentAccessScopeInput AccessScope);
