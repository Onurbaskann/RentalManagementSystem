namespace KiraTakip.Domain.Leases;

public sealed record LeaseGroupShare(int TenantId, decimal Area, DateTime StartDate, DateTime EndDate);

public enum LeaseGroupViolation
{
    TooFewLeases = 1,
    DuplicateTenant = 2,
    AreaSumMismatch = 3,
    DatesNotShared = 4
}

public static class LeaseGroupPolicy
{
    public static IReadOnlyList<LeaseGroupViolation> GetViolations(
        decimal unitArea,
        IReadOnlyCollection<LeaseGroupShare> shares)
    {
        var violations = new List<LeaseGroupViolation>();

        if (shares.Count < 2)
            violations.Add(LeaseGroupViolation.TooFewLeases);

        if (shares.Select(s => s.TenantId).Distinct().Count() != shares.Count)
            violations.Add(LeaseGroupViolation.DuplicateTenant);

        if (shares.Sum(s => s.Area) != unitArea)
            violations.Add(LeaseGroupViolation.AreaSumMismatch);

        if (shares.Select(s => s.StartDate.Date).Distinct().Count() > 1
            || shares.Select(s => s.EndDate.Date).Distinct().Count() > 1)
            violations.Add(LeaseGroupViolation.DatesNotShared);

        return violations;
    }

    public static bool IsValidRestructureStart(DateTime terminationDate, DateTime newStartDate)
        => newStartDate.Date > terminationDate.Date;

    public static DateTime SuggestRestructureStart(DateTime terminationDate)
        => terminationDate.Date.AddDays(1);
}
