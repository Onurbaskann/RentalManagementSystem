namespace KiraTakip.Services.Interfaces.Pricing;

public record RateResolutionRequest(
    int LeaseId,
    int TenantId,
    int UnitId,
    int ChargeTypeId,
    DateTime Period);

// RateResolverService.ResolveAsync'in "leaseId dolu" kademe/kısa devre mantığını (tek gerçek
// çağıran yolu — bkz. StatisticsService.GetMonthlyAmountAsync) toplu (bulk) olarak çözer: çok
// sayıda istek için tek seferde bulk-yüklenmiş veriden bellekte hesaplar. Yalnız görüntüleme
// amaçlı toplu hesaplamalar için (ör. Property/Details); billing-kritik akışlar
// IRateResolverService'i kullanmaya devam eder.
public interface IBatchRateResolver
{
    Task<Dictionary<(int LeaseId, int ChargeTypeId), RateSnapshot?>> ResolveManyAsync(
        IReadOnlyCollection<RateResolutionRequest> requests);
}
