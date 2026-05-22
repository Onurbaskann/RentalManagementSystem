namespace KiraTakip.Models.ViewModels;

public class TasinmazYetkiCheckboxViewModel
{
    public int TasinmazId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Konum { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
