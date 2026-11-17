namespace KiraTakip.Services.Interfaces.Leases;

/// <summary>Sözleşme depozitosunun iade edilip edilmediğini bildirir (yeniden yapılanma onay ön koşulu).</summary>
public interface IDepositRefundStatusService
{
    Task<bool> IsRefundedAsync(int leaseId);
}
