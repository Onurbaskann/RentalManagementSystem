namespace KiraTakip.Models.Dtos;

public sealed record ChargeLineItemPaymentBalanceDto(
    int ChargeLineItemId,
    int ChargeId,
    int ChargeTypeId,
    int UnitId,
    int TenantId,
    string ChargeTypeName,
    string Description,
    decimal TotalAmount,
    decimal ApprovedAmount,
    decimal PendingAmount)
{
    public decimal RemainingAmount => TotalAmount - ApprovedAmount;
    public decimal AvailableAmount => TotalAmount - ApprovedAmount - PendingAmount;
}
