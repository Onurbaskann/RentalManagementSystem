namespace KiraTakip.Services.Interfaces;

public interface IChargeGenerationService
{
    Task UretSozlesmeIcinAsync(int leaseId);
    Task YenidenUretAsync(int leaseId, DateTime baslangicTarihi);
    Task IptalEtFutureTahakkuklarAsync(int leaseId, DateTime fesihTarihi);
    Task BekleyenVadeleriYenidenHesaplaAsync(int leaseId);
    Task<IList<Models.DTOs.TahakkukKalemiPreview>> ComposeKalemlerAsync(int unitId, int tenantId, DateTime donem, int? leaseId = null);
}
