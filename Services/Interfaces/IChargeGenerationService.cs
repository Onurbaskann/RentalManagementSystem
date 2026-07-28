using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IChargeGenerationService
{
    Task GenerateForLeaseAsync(GenerateLeaseChargesInput input);
    Task RegenerateAsync(RegenerateLeaseChargesInput input);
    Task CancelFutureChargesAsync(CancelFutureLeaseChargesInput input);
    Task RecalculatePendingDueDatesAsync(RecalculateLeaseDueDatesInput input);
    Task<IList<ChargeLineItemPreview>> ComposeLineItemsAsync(ComposeLeaseLineItemsInput input);
}
