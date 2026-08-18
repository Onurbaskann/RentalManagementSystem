using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KiraTakip.Services;

public sealed class ReservationCompletionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationCompletionSettings> options,
    ILogger<ReservationCompletionBackgroundService> logger) : BackgroundService
{
    private readonly ReservationCompletionSettings _settings = Validate(options.Value);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Rezervasyon otomatik tamamlama görevi devre dışı.");
            return;
        }

        logger.LogInformation(
            "Rezervasyon otomatik tamamlama görevi başladı. Aralık: {IntervalMinutes} dakika, batch: {BatchSize}.",
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
                    .GetRequiredService<IReservationCompletionService>();
                candidateIds = await service.FindCandidatesAsync(
                    new FindReservationCompletionCandidatesInput(_settings.BatchSize));
            }

            var completedCount = 0;
            foreach (var reservationId in candidateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var itemScope = scopeFactory.CreateScope();
                    var service = itemScope.ServiceProvider
                        .GetRequiredService<IReservationCompletionService>();
                    if (await service.CompleteAsync(new CompleteReservationInput(reservationId)))
                        completedCount++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Rezervasyon {ReservationId} otomatik tamamlanamadı; sonraki periyotta yeniden denenecek.",
                        reservationId);
                }
            }

            if (completedCount > 0)
            {
                logger.LogInformation(
                    "Rezervasyon otomatik tamamlama turunda {CompletedCount} kayıt tamamlandı.",
                    completedCount);
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
                "Rezervasyon otomatik tamamlama batch'i başarısız oldu; sonraki periyotta yeniden denenecek.");
        }
    }

    private static ReservationCompletionSettings Validate(
        ReservationCompletionSettings settings)
    {
        if (settings.IntervalMinutes <= 0 || settings.BatchSize <= 0)
            throw new InvalidOperationException(
                "Rezervasyon tamamlama aralığı ve batch boyutu pozitif olmalıdır.");
        return settings;
    }
}
