namespace KiraTakip.Models.ViewModels;

public class TarifeYilEkleViewModel
{
    public int Yil { get; set; } = DateTime.Now.Year;
    public int? KopyalaYil { get; set; }
}
