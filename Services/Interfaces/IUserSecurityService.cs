namespace KiraTakip.Services.Interfaces;

public interface IUserSecurityService
{
    Task UpdateStampAsync(string userId);
    Task UpdateStampForRoleUsersAsync(int rolId);
}
