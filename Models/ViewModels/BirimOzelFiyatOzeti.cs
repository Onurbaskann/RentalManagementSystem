namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatOzeti
{
    public Unit Unit { get; set; } = null!;
    public List<BirimTarife> Rateler { get; set; } = [];
}
