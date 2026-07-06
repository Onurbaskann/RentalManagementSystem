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
    public string? Durum { get; set; }
    public int? TasinmazId { get; set; }
    public int? BirimId { get; set; }
    public int? KiraciId { get; set; }
    public string? Kaynak { get; set; }
    public int? Yil { get; set; }

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
        if (!string.IsNullOrWhiteSpace(Durum) && Durum != "tum") d["durum"] = Durum;
        
        if (TasinmazId.HasValue) d["propertyId"] = TasinmazId.ToString();
        if (BirimId.HasValue) d["unitId"] = BirimId.ToString();
        if (KiraciId.HasValue) d["tenantId"] = KiraciId.ToString();
        if (!string.IsNullOrWhiteSpace(Kaynak)) d["kaynak"] = Kaynak;
        if (Yil.HasValue) d["yil"] = Yil.ToString();
        
        return d;
    }
}
