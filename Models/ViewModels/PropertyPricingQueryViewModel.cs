namespace KiraTakip.Models.ViewModels;

public class PropertyPricingQueryViewModel
{
    public int PropertyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
