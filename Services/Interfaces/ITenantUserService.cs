using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITenantUserService
{
    Task EnsureTenantManagerExistsAsync(
        EnsureTenantManagerExistsInput input,
        CancellationToken ct = default);
    Task<TenantUsersListDto> GetTenantUsersListAsync(GetTenantUsersListInput input);
    Task ToggleUserActiveAsync(ToggleTenantUserActiveInput input);
    Task CancelInvitationAsync(CancelTenantInvitationInput input);
    Task ResendInvitationAsync(ResendTenantInvitationInput input);
    Task<TenantInviteDataDto> GetInviteDataAsync(GetInviteDataInput input);
    Task SendInvitationAsync(SendTenantInvitationInput input);
    Task<InitialTenantInvitationResultDto> TrySendInitialRepresentativeInvitationAsync(
        SendInitialTenantRepresentativeInput input);
    Task<TenantUserEditDataDto> GetTenantUserForEditAsync(GetTenantUserForEditInput input);
    Task EditTenantUserAsync(EditTenantUserInput input);

}
