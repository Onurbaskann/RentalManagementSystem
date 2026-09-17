namespace KiraTakip.Web.Authorization;

public interface ICurrentUserPermissionContext
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetPermissionsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
