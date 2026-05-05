namespace KiraTakip.Models.Common;

public class PaginationModel
{
    public int Page { get; set; }
    public int Size { get; set; } = 25;
    public int Total { get; set; }
    public string BasePath { get; set; } = "";
    public Dictionary<string, string?> Extra { get; set; } = new();

    public int TotalPages => Size > 0 ? Math.Max(1, (int)Math.Ceiling(Total / (double)Size)) : 1;
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int From => Total == 0 ? 0 : (Page - 1) * Size + 1;
    public int To => Math.Min(Page * Size, Total);

    public string Url(int page)
    {
        var q = new List<string> { $"page={page}" };
        if (Size != 25) q.Add($"size={Size}");
        foreach (var kv in Extra)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            q.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        }
        return $"{BasePath}?{string.Join("&", q)}";
    }

    public IEnumerable<int> PageNumbers()
    {
        int total = TotalPages;
        int current = Page;
        var pages = new HashSet<int> { 1, total };
        for (int i = current - 2; i <= current + 2; i++)
            if (i >= 1 && i <= total) pages.Add(i);
        return pages.OrderBy(x => x);
    }

    public static PaginationModel FromPaged<T>(PagedResult<T> r, string basePath, Dictionary<string, string?>? extra = null)
        => new() { Page = r.Page, Size = r.Size, Total = r.Total, BasePath = basePath, Extra = extra ?? new() };
}
