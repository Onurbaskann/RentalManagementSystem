using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.TenantPanel;

namespace KiraTakip.Services.Interfaces.Tenants;

public interface ITenantPanelService
{
    Task<TenantPanelDashboardDto> GetDashboardAsync(GetTenantPanelDashboardInput input);
}
