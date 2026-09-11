namespace KiraTakip.Services.Interfaces.Identity;

public interface IUserSecurityService
{
    Task UpdateStampAsync(string userId);
    Task UpdateStampForRoleUsersAsync(int rolId);
}
