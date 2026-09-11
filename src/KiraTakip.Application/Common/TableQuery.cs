namespace KiraTakip.Models.Common;

public class TableQuery
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Q { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string? Status { get; set; }
    public int? PropertyId { get; set; }
    public int? UnitId { get; set; }
    public int? TenantId { get; set; }
    public string? Source { get; set; }
    public int? Year { get; set; }

    public int Skip => (Math.Max(1, Page) - 1) * SafeSize;
    public int Take => SafeSize;
    public int SafeSize => Math.Max(1, Math.Min(200, Size));

    public Dictionary<string, string?> ToQueryDict()
    {
        var d = new Dictionary<string, string?>();
        if (Size != 10) d["size"] = Size.ToString();
        if (!string.IsNullOrWhiteSpace(Q)) d["q"] = Q;
        if (From.HasValue) d["from"] = From.Value.ToString("yyyy-MM-dd");
        if (To.HasValue) d["to"] = To.Value.ToString("yyyy-MM-dd");
        if (Min.HasValue) d["min"] = Min.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (Max.HasValue) d["max"] = Max.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(Status) && Status != "tum") d["status"] = Status;
        
        if (PropertyId.HasValue) d["propertyId"] = PropertyId.ToString();
        if (UnitId.HasValue) d["unitId"] = UnitId.ToString();
        if (TenantId.HasValue) d["tenantId"] = TenantId.ToString();
        if (!string.IsNullOrWhiteSpace(Source)) d["source"] = Source;
        if (Year.HasValue) d["year"] = Year.ToString();
        
        return d;
    }
}
