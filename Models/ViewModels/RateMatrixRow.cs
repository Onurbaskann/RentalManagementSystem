namespace KiraTakip.Models.ViewModels;

public class RateMatrixRow
{
    public int TenantCategoryId { get; set; }
    public string TenantCategoryName { get; set; } = string.Empty;
    public List<RateMatrixCell> Cells { get; set; } = [];
}
