using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public sealed class ReservationPolicyProvider(IServiceScopeFactory scopeFactory)
    : IReservationPolicyProvider
{
    private ReservationPolicySettings? _current;

    public ReservationPolicySettings Current
        => Volatile.Read(ref _current)
           ?? throw new InvalidOperationException("Rezervasyon sistem ayarları henüz yüklenmedi.");

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISystemSettingRepository>();
        var keys = SystemSettingDefinitions.All
            .Where(definition => definition.Key.StartsWith("Reservation.", StringComparison.OrdinalIgnoreCase))
            .Select(definition => definition.Key)
            .ToArray();
        var settings = await repository.GetActiveByKeysAsync(keys, cancellationToken);
        var values = settings.ToDictionary(
            setting => setting.Key,
            setting => setting.Value,
            StringComparer.OrdinalIgnoreCase);
        var policy = SystemSettingDefinitions.CreateReservationPolicy(values);

        Volatile.Write(ref _current, policy);
    }
}
