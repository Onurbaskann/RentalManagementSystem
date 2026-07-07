namespace KiraTakip.Services.Interfaces;

public interface IChargeGenerationService
{
    Task GenerateForLeaseAsync(int leaseId);
    Task RegenerateAsync(int leaseId, DateTime startDate);
    Task CancelFutureChargesAsync(int leaseId, DateTime terminationDate);
    Task RecalculatePendingDueDatesAsync(int leaseId);
    Task<IList<Models.DTOs.ChargeLineItemPreview>> ComposeLineItemsAsync(int unitId, int tenantId, DateTime period, int? leaseId = null);
}
