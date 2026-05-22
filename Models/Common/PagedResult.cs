namespace KiraTakip.Models.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 25;

    public int TotalPages => Size > 0 ? (int)Math.Ceiling(Total / (double)Size) : 0;
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int From => Total == 0 ? 0 : (Page - 1) * Size + 1;
    public int To => Math.Min(Page * Size, Total);
}
