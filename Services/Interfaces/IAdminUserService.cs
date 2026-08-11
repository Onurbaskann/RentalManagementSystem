using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IAdminUserService
{
    Task<AdminUserIndexDto> GetIndexAsync();
    Task<AdminUserIndexPageDto> GetIndexPageAsync(GetAdminUserIndexPageInput input);
    Task<AdminUserEditDataDto?> GetEditDataAsync(GetAdminUserEditDataInput input);
    Task<AdminUserFormOptionsDto> GetFormOptionsAsync();
    Task UpdateAsync(UpdateAdminUserInput input);
    Task ToggleActiveAsync(ToggleAdminUserActiveInput input);
    Task SendInvitationAsync(SendAdminUserInvitationInput input);
    Task CancelInvitationAsync(CancelAdminUserInvitationInput input);
    Task ResendInvitationAsync(ResendAdminUserInvitationInput input);
}
