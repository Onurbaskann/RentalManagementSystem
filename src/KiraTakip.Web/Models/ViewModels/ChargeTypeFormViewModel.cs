using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class ChargeTypeFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ChargeTypeBehavior Behavior { get; set; } = ChargeTypeBehavior.MonthlyFixed;

    public int SortOrder { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public bool IsSystem { get; set; }
}
