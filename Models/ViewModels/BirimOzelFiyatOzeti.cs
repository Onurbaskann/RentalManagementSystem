using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatOzeti
{
    public Unit Unit { get; set; } = null!;
    public List<UnitRate> Rateler { get; set; } = [];
}
