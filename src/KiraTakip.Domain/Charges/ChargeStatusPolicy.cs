using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Charges;

public static class ChargeStatusPolicy
{
    public static ChargeStatus DetermineStatus(
        decimal paidAmount,
        decimal totalAmount,
        DateTime dueDate,
        DateTime today)
    {
        if (paidAmount >= totalAmount)
            return ChargeStatus.Paid;

        if (paidAmount > 0)
            return ChargeStatus.PartiallyPaid;

        if (IsOverdue(dueDate, today))
            return ChargeStatus.Overdue;

        return ChargeStatus.Pending;
    }

    public static bool IsOverdue(DateTime dueDate, DateTime today)
        => today > dueDate;
}
