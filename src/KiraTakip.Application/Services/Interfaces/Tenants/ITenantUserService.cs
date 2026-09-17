using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.TenantUser;

namespace KiraTakip.Services.Interfaces.Tenants;

public interface ITenantUserService
{
    Task EnsureTenantManagerExistsAsync(
        EnsureTenantManagerExistsInput input,
        CancellationToken ct = default);
    Task<TenantUsersListDto> GetTenantUsersListAsync(GetTenantUsersListInput input);
    Task<TenantUsersPageDto> GetTenantUsersPageAsync(GetTenantUsersPageInput input);
    Task ToggleUserActiveAsync(ToggleTenantUserActiveInput input);
    Task CancelInvitationAsync(CancelTenantInvitationInput input);
    Task ResendInvitationAsync(ResendTenantInvitationInput input);
    Task<TenantInviteDataDto> GetInviteDataAsync(GetInviteDataInput input);
    Task SendInvitationAsync(SendTenantInvitationInput input);
    Task<InitialTenantInvitationResultDto> TrySendInitialRepresentativeInvitationAsync(
        SendInitialTenantRepresentativeInput input);
    Task<TenantUserEditDataDto> GetTenantUserForEditAsync(GetTenantUserForEditInput input);
    Task<List<RoleLookupDto>> GetEditRoleOptionsAsync(int tenantId, int currentRoleId, string? actorUserId = null);
    Task EditTenantUserAsync(EditTenantUserInput input);
}
