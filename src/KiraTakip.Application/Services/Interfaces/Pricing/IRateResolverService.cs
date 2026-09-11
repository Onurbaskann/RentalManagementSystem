using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Pricing;

public class RateSnapshot
{
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
    public LineItemSourceType SourceType { get; set; }
}

public interface IRateResolverService
{
    Task<RateSnapshot?> ResolveAsync(int? leaseId, int? tenantId, int unitId, int chargeTypeId, DateTime donem);
}
