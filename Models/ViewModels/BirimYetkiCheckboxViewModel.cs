namespace KiraTakip.Models.ViewModels;

public class BirimYetkiCheckboxViewModel
{
    public int BirimId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string TasinmazAd { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
