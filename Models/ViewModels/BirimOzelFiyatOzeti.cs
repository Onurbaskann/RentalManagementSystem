namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatOzeti
{
    public Birim Birim { get; set; } = null!;
    public List<BirimTarife> Rateler { get; set; } = [];
}
