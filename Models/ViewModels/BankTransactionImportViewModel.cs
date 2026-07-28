namespace KiraTakip.Models.ViewModels;

public class BankTransactionImportViewModel
{
    public string BankCode { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
