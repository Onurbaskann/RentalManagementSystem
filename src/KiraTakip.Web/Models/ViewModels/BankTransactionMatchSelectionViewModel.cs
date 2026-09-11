using KiraTakip.Models.Dtos;

namespace KiraTakip.Web.Models.ViewModels;

public class BankTransactionMatchSelectionViewModel
{
    public BankTransactionDetailDto BankTransaction { get; set; } = null!;
    public List<PaymentCandidateDto> PaymentCandidates { get; set; } = [];
}
