using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITenantPanelService
{
    Task<TenantPanelDashboardDto> GetDashboardAsync(GetTenantPanelDashboardInput input);
}
