using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.TenantPanel;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Repositories.Interfaces.Charges;

public interface IChargeLineItemRepository : IRepositoryBase<ChargeLineItem>
{
    Task<Dictionary<int, decimal?>> GetDepositAmountsByLeaseIdsAsync(
        IEnumerable<int> leaseIds,
        int? tenantId = null);
    Task<List<TenantPanelDebtSliceDto>> GetTenantDebtDistributionAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<ChargeLineItemPaymentBalanceDto?> GetPaymentBalanceAsync(int chargeLineItemId, CancellationToken cancellationToken = default);
    Task<List<ChargeLineItemPaymentBalanceDto>> GetPaymentBalancesByChargeAsync(int chargeId, CancellationToken cancellationToken = default);
    Task<ChargeLineItem?> GetForPaymentUpdateAsync(int chargeLineItemId);
    Task<decimal> GetChargePaidAmountTotalAsync(int chargeId);
    Task AcquirePaymentLockAsync(int chargeLineItemId);
}
