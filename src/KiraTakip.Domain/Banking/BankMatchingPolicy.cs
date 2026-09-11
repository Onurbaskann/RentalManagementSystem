using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Banking;

public static class BankMatchingPolicy
{
    public static bool CanMatchPayment(PaymentStatus status)
        => status is PaymentStatus.PendingApproval or PaymentStatus.Approved;

    public static bool CanMatchTransaction(BankMatchStatus matchStatus)
        => matchStatus == BankMatchStatus.Unmatched;

    public static bool AreStoreAccountsCompatible(int? paymentStoreAccountId, int? transactionStoreAccountId)
        => paymentStoreAccountId == transactionStoreAccountId;
}
