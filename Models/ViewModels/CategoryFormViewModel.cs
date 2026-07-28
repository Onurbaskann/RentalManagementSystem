using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class CategoryFormViewModel
{
    public int Id { get; set; }
    public CategoryType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
