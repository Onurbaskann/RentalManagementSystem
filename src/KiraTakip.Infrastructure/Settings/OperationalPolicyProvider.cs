using Microsoft.Extensions.DependencyInjection;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Settings;
using KiraTakip.Services.Interfaces.Payments;

namespace KiraTakip.Services.Payments;

public sealed class OperationalPolicyProvider(IServiceScopeFactory scopeFactory)
    : IOperationalPolicyProvider
{
    private OperationalPolicySettings? current;

    public OperationalPolicySettings Current
        => Volatile.Read(ref current)
           ?? throw new InvalidOperationException("Operasyonel sistem ayarları henüz yüklenmedi.");

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISystemSettingRepository>();
        var keys = SystemSettingDefinitions.All
            .Where(definition =>
                !definition.Key.StartsWith("Reservation.", StringComparison.OrdinalIgnoreCase))
            .Select(definition => definition.Key)
            .ToArray();
        var settings = await repository.GetActiveByKeysAsync(keys, cancellationToken);
        var values = settings.ToDictionary(
            setting => setting.Key,
            setting => setting.Value,
            StringComparer.OrdinalIgnoreCase);

        Volatile.Write(
            ref current,
            SystemSettingDefinitions.CreateOperationalPolicy(values));
    }
}
