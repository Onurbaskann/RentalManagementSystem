namespace KiraTakip.Services.Identity;

public interface IPermissionCacheTransactionState
{
    void AddPending(Guid transactionId, string userId);
    void AddPendingRange(Guid transactionId, IEnumerable<string> userIds);
    IReadOnlyList<string> RemovePending(Guid transactionId);
    void ClearPending(Guid transactionId);
    void ClearAllPending();
}

public class PermissionCacheTransactionState : IPermissionCacheTransactionState, IDisposable
{
    private readonly Dictionary<Guid, HashSet<string>> _pending = new();

    public void AddPending(Guid transactionId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        if (!_pending.TryGetValue(transactionId, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _pending[transactionId] = set;
        }

        set.Add(userId);
    }

    public void AddPendingRange(Guid transactionId, IEnumerable<string> userIds)
    {
        if (userIds == null) return;

        if (!_pending.TryGetValue(transactionId, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _pending[transactionId] = set;
        }

        foreach (var userId in userIds)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                set.Add(userId);
            }
        }
    }

    public IReadOnlyList<string> RemovePending(Guid transactionId)
    {
        if (_pending.Remove(transactionId, out var set))
        {
            return set.ToList();
        }

        return [];
    }

    public void ClearPending(Guid transactionId)
    {
        _pending.Remove(transactionId);
    }

    public void ClearAllPending()
    {
        _pending.Clear();
    }

    public void Dispose()
    {
        _pending.Clear();
    }
}
