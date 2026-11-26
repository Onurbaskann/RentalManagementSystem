namespace KiraTakip.Repositories.Interfaces.Spaces;

public enum SpaceLockMode
{
    Shared = 1,
    Exclusive = 2
}

public interface ISpaceLockRepository
{
    Task AcquireRootLockAsync(IEnumerable<int> rootIds, SpaceLockMode mode = SpaceLockMode.Exclusive);
}
