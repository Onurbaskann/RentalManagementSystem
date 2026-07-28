namespace KiraTakip.Models.Dtos;

public record CreatePaymentLinkInput(int TenantId);

public record ValidatePaymentLinkInput(string Token);

public record CancelPaymentLinkInput(int RecordId, string CancelledByUserId);

public record PaymentLinkValidationResultDto(bool Success, int TenantId, string? Reason);
