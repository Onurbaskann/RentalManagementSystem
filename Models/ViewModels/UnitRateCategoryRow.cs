namespace KiraTakip.Models.ViewModels;

public class UnitRateCategoryRow
{
    public int TenantCategoryId { get; set; }
    public string TenantCategoryName { get; set; } = string.Empty;
    public List<UnitRateCell> Hucreler { get; set; } = [];
}
