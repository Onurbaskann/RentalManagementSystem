using KiraTakip.Models.Settings;

namespace KiraTakip.Services.Interfaces;

public interface IReservationPolicyProvider
{
    ReservationPolicySettings Current { get; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
