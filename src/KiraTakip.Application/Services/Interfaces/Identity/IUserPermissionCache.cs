namespace KiraTakip.Services.Interfaces.Identity;

public interface IUserPermissionCache
{
    Task<IReadOnlySet<string>> GetAsync(string userId, CancellationToken cancellationToken = default);
    void Invalidate(string userId);
    void InvalidateMany(IEnumerable<string> userIds);
}
