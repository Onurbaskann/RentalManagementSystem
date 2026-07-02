using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public class RateSnapshot
{
    public CalculationMethod CalculationMethod { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
    public LineItemSourceType KaynakTipi { get; set; }
}

public interface IRateResolverService
{
    Task<RateSnapshot?> ResolveAsync(int? sozlesmeId, int? kiraciId, int birimId, int borcTipiId, DateTime donem);
}
