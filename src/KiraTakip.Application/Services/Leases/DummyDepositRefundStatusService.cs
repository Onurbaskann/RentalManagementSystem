using KiraTakip.Services.Interfaces.Leases;

namespace KiraTakip.Services.Leases;

/// <summary>Depozito iade modülü gelene kadar her sözleşme için iade edilmiş kabul eder.</summary>
public class DummyDepositRefundStatusService : IDepositRefundStatusService
{
    public Task<bool> IsRefundedAsync(int leaseId) => Task.FromResult(true);
}
