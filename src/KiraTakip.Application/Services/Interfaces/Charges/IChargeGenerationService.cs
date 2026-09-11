using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Services.Interfaces.Charges;

public interface IChargeGenerationService
{
    Task GenerateForLeaseAsync(GenerateLeaseChargesInput input);
    Task RegenerateAsync(RegenerateLeaseChargesInput input);
    Task CancelFutureChargesAsync(CancelFutureLeaseChargesInput input);
    Task RecalculatePendingDueDatesAsync(RecalculateLeaseDueDatesInput input);
    Task<IList<ChargeLineItemPreview>> ComposeLineItemsAsync(ComposeLeaseLineItemsInput input);
}
