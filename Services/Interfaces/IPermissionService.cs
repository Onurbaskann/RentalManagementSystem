namespace KiraTakip.Services.Interfaces;

public interface IPermissionService
{
    Task<IList<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task SetUserPermissionsAsync(string userId, IEnumerable<string> permissions, string grantedByUserId);
}
