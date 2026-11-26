namespace KiraTakip.Services.Interfaces.Pricing;

public interface ISpaceRateResolver
{
    // Alanın kendi tarifesi > en yakın ata > genel tarife (yıl düşmesi dahil). Sözleşme override bu katmanda yok.
    Task<RateSnapshot?> ResolveAsync(int spaceId, int tenantCategoryId, int chargeTypeId, DateTime donem);
}
