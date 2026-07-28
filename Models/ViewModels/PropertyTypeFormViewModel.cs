namespace KiraTakip.Models.ViewModels;

public class PropertyTypeFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool SupportsSingleUnit { get; set; }
    public bool SupportsMultipleUnits { get; set; }
}
