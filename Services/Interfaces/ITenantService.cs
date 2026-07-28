using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITenantService
{
    Task<List<TenantListItemDto>> GetAllAsync(GetTenantsInput input);
    Task<TenantDetailsDto?> GetDetailsAsync(GetTenantDetailsInput input);
    Task<TenantDetailsDto> GetProfileAsync(GetTenantProfileInput input);
    Task<CreatedTenantDto> CreateAsync(CreateTenantInput input);
    Task UpdateAsync(UpdateTenantInput input);
    Task<string> GenerateTenantNoAsync();
    Task<bool> IsInactiveAsync(CheckTenantInactiveInput input, CancellationToken ct = default);
}
