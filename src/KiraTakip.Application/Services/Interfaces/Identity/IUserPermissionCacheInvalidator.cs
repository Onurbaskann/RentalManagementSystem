namespace KiraTakip.Services.Interfaces.Identity;

public interface IUserPermissionCacheInvalidator
{
    void InvalidateAfterCommit(string userId);
    void InvalidateManyAfterCommit(IEnumerable<string> userIds);
}
