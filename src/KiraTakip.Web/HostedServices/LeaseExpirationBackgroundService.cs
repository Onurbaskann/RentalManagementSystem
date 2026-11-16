using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces.Leases;
using Microsoft.Extensions.Options;

namespace KiraTakip.Web.HostedServices;

public sealed class LeaseExpirationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<LeaseExpirationSettings> options,
    ILogger<LeaseExpirationBackgroundService> logger) : BackgroundService
{
    private readonly LeaseExpirationSettings _settings = Validate(options.Value);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Sözleşme otomatik sona erdirme görevi devre dışı.");
            return;
        }

        logger.LogInformation(
            "Sözleşme otomatik sona erdirme görevi başladı. Aralık: {IntervalMinutes} dakika, batch: {BatchSize}.",
            _settings.IntervalMinutes,
            _settings.BatchSize);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.IntervalMinutes));
        do
        {
            await ProcessBatchSafelyAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<int> candidateIds;
            using (var candidateScope = scopeFactory.CreateScope())
            {
                var service = candidateScope.ServiceProvider
                    .GetRequiredService<ILeaseExpirationService>();
                candidateIds = await service.FindCandidatesAsync(
                    _settings.BatchSize);
            }

            var expiredCount = 0;
            foreach (var leaseId in candidateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var itemScope = scopeFactory.CreateScope();
                    var service = itemScope.ServiceProvider
                        .GetRequiredService<ILeaseExpirationService>();
                    if (await service.ExpireAsync(leaseId))
                        expiredCount++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Sözleşme {LeaseId} otomatik sona erdirilemedi; sonraki periyotta yeniden denenecek.",
                        leaseId);
                }
            }

            if (expiredCount > 0)
            {
                logger.LogInformation(
                    "Sözleşme otomatik sona erdirme turunda {ExpiredCount} sözleşme sona erdirildi.",
                    expiredCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Sözleşme otomatik sona erdirme batch'i başarısız oldu; sonraki periyotta yeniden denenecek.");
        }
    }

    private static LeaseExpirationSettings Validate(
        LeaseExpirationSettings settings)
    {
        if (settings.IntervalMinutes <= 0 || settings.BatchSize <= 0)
            throw new InvalidOperationException(
                "Sözleşme sona erdirme aralığı ve batch boyutu pozitif olmalıdır.");
        return settings;
    }
}
