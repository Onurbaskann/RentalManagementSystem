namespace KiraTakip.Domain.Tenants;

public enum UpcomingChargeUrgency
{
    Overdue = 1,
    DueSoon = 2,
    Normal = 3
}

public static class TenantPanelSchedulePolicy
{
    public static int CalculateDueDayDifference(DateTime dueDate, DateTime today)
        => (dueDate.Date - today.Date).Days;

    public static UpcomingChargeUrgency DetermineUpcomingChargeUrgency(int dayDifference)
    {
        if (dayDifference < 0)
            return UpcomingChargeUrgency.Overdue;

        if (dayDifference <= 7)
            return UpcomingChargeUrgency.DueSoon;

        return UpcomingChargeUrgency.Normal;
    }

    public static UpcomingChargeUrgency DetermineUpcomingChargeUrgency(DateTime dueDate, DateTime today)
        => DetermineUpcomingChargeUrgency(CalculateDueDayDifference(dueDate, today));
}
