using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.Tenant;

namespace KiraTakip.Services.Interfaces.Tenants;

public interface ITenantService
{
    Task<List<TenantListItemDto>> GetAllAsync(GetTenantsInput input);
    Task<PagedResult<TenantListItemDto>> GetPagedAsync(GetPagedTenantsInput input);
    Task<TenantDetailsDto?> GetDetailsAsync(GetTenantDetailsInput input);
    Task<TenantDetailsDto> GetProfileAsync(GetTenantProfileInput input);
    Task<CreatedTenantDto> CreateAsync(CreateTenantInput input);
    Task UpdateAsync(UpdateTenantInput input);
    Task<string> GenerateTenantNoAsync();
    Task<bool> IsInactiveAsync(CheckTenantInactiveInput input, CancellationToken ct = default);
}
