using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IChargeLineItemRepository : IBaseRepository<ChargeLineItem>
{
    Task<Dictionary<int, decimal?>> GetDepositAmountsByLeaseIdsAsync(
        IEnumerable<int> leaseIds,
        int? tenantId = null);
    Task<List<TenantPanelDebtSliceDto>> GetTenantDebtDistributionAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
}
