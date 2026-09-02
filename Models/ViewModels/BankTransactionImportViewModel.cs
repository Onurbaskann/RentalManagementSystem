using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Models.ViewModels;

public class BankTransactionImportViewModel
{
    public string BankCode { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public IFormFile? File { get; set; }
    public List<StoreRoutingOptionDto> StoreOptions { get; set; } = [];
}
