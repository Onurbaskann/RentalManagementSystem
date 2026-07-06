namespace KiraTakip.Models.ViewModels;

public class BankaImportViewModel
{
    public string BankCode { get; set; } = string.Empty;
    public IFormFile? Dosya { get; set; }
}
