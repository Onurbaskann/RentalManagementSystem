using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public class RateSnapshot
{
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
    public KaynakTipi KaynakTipi { get; set; }
}

public interface IRateResolverService
{
    Task<RateSnapshot?> ResolveAsync(int? sozlesmeId, int? kiraciId, int birimId, int borcTipiId, DateTime donem);
}
