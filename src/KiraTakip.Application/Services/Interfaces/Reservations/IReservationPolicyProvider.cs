using KiraTakip.Models.Settings;

namespace KiraTakip.Services.Interfaces.Reservations;

public interface IReservationPolicyProvider
{
    ReservationPolicySettings Current { get; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
