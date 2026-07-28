namespace KiraTakip.Models.Dtos;

public record GetPaymentPortalInput(string Token);

public record PaymentPortalResultDto(
    bool Success,
    string? FailureReason,
    string TenantName,
    IReadOnlyList<PaymentPortalChargeDto> Charges);

public record PaymentPortalChargeDto(
    int ChargeId,
    string PropertyName,
    string UnitName,
    DateTime PeriodStart,
    DateTime DueDate,
    decimal TotalAmount,
    decimal PaidAmount);
