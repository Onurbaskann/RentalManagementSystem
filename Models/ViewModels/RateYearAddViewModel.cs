namespace KiraTakip.Models.ViewModels;

public class RateYearAddViewModel
{
    public int Year { get; set; } = DateTime.Now.Year;
    public int? CopyFromYear { get; set; }
}
