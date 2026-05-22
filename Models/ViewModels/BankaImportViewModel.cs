namespace KiraTakip.Models.ViewModels;

public class BankaImportViewModel
{
    public string BankaKodu { get; set; } = string.Empty;
    public IFormFile? Dosya { get; set; }
}
