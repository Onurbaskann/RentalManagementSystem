using KiraTakip.Web.Help;

namespace KiraTakip.Web.Models.ViewModels;

public class HelpIndexViewModel
{
    public bool IsTenant { get; set; }
    public IReadOnlyList<HelpSection> Sections { get; set; } = [];
}
