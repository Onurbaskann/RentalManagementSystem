using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Tests;

internal sealed class TestOperationalPolicyProvider(
    OperationalPolicySettings? settings = null) : IOperationalPolicyProvider
{
    public int RefreshCount { get; private set; }
    public OperationalPolicySettings Current { get; } = settings ?? new();

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        RefreshCount++;
        return Task.CompletedTask;
    }
}
