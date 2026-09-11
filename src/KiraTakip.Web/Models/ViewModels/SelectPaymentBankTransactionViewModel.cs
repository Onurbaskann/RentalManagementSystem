using KiraTakip.Models.Dtos;

namespace KiraTakip.Web.Models.ViewModels;

public class SelectPaymentBankTransactionViewModel
{
    public PaymentDetailDto Payment { get; set; } = null!;
    public List<BankTransactionListItemDto> TransactionCandidates { get; set; } = [];
}
