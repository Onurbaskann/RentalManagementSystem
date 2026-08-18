using KiraTakip.Services.Interfaces;

namespace KiraTakip.Infrastructure.DependencyInjection;

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
