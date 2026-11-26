using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceLockRepository(ApplicationDbContext ctx) : ISpaceLockRepository
{
    private const string RootResourcePrefix = "KiraTakip.Space.Root.";

    public async Task AcquireRootLockAsync(IEnumerable<int> rootIds, SpaceLockMode mode = SpaceLockMode.Exclusive)
    {
        if (ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Alan kilidi aktif transaction gerektirir.");

        var lockMode = mode == SpaceLockMode.Shared ? nameof(SpaceLockMode.Shared) : nameof(SpaceLockMode.Exclusive);
        foreach (var rootId in rootIds.Distinct().Order())
            await AcquireAppLockAsync($"{RootResourcePrefix}{rootId}", lockMode);
    }

    private Task AcquireAppLockAsync(string resource, string mode)
        => ctx.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = {mode},
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            IF @result < 0
                THROW 51000, 'Alan kilidi alınamadı.', 1;");
}
