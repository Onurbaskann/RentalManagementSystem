using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reservations;

namespace KiraTakip.Web.Hosting;

public static class SystemSettingInitializationExtensions
{
    public static async Task InitializeSystemSettingsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await app.Services
            .GetRequiredService<IReservationPolicyProvider>()
            .RefreshAsync(cancellationToken);
        await app.Services
            .GetRequiredService<IOperationalPolicyProvider>()
            .RefreshAsync(cancellationToken);
    }
}
