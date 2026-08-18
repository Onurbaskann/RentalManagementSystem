using KiraTakip.Models.Settings;

namespace KiraTakip.Services.Interfaces;

public interface IOperationalPolicyProvider
{
    OperationalPolicySettings Current { get; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
