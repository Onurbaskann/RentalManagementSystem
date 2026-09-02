using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentStoreRoutingService
{
    Task<PaymentStoreRoutingIndexDataDto> GetManagementDataAsync(TableQuery query);
    Task UpsertAsync(UpsertPaymentStoreRoutingInput input);
    Task DeactivateOverrideAsync(int id);
    Task<int?> GetDefaultStoreIdAsync(int chargeTypeId);
    Task<bool> HasUsableDefaultAsync(int chargeTypeId);
}
