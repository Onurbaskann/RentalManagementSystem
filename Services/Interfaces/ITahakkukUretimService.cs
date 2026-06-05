namespace KiraTakip.Services.Interfaces;

public interface ITahakkukUretimService
{
    Task UretSozlesmeIcinAsync(int sozlesmeId);
    Task YenidenUretAsync(int sozlesmeId, DateTime baslangicTarihi);
    Task IptalEtFutureTahakkuklarAsync(int sozlesmeId, DateTime fesihTarihi);
    Task BekleyenVadeleriYenidenHesaplaAsync(int sozlesmeId);
    Task<IList<Models.DTOs.TahakkukKalemiPreview>> ComposeKalemlerAsync(int birimId, int kiraciId, DateTime donem, int? sozlesmeId = null);
}
