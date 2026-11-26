using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Spaces;

public sealed record SpaceState(
    int? ParentId,
    SpaceUsage Usage,
    decimal Area,
    bool IsPreAllocatable,
    bool IsShared);

public sealed record OpenRecordFlags(bool OpenLease, bool ActivePreAllocation, bool OpenReservation)
{
    public static readonly OpenRecordFlags None = new(false, false, false);

    public bool Any => OpenLease || ActivePreAllocation || OpenReservation;
}

public sealed record SpaceOpenRecords(OpenRecordFlags OnSelf, OpenRecordFlags OnAncestors, OpenRecordFlags OnSubtree)
{
    public static readonly SpaceOpenRecords None = new(OpenRecordFlags.None, OpenRecordFlags.None, OpenRecordFlags.None);

    public bool Any => OnSelf.Any || OnAncestors.Any || OnSubtree.Any;

    public bool AnyOpenReservation => OnSelf.OpenReservation || OnAncestors.OpenReservation || OnSubtree.OpenReservation;
}

public enum SpaceChangeViolation
{
    ParentChangeNotAllowed = 1,
    AreaNotPositive = 2,
    AreaChangeWithOpenLease = 3,
    UsageChangeWithOpenRecords = 4,
    UsageChangeWithOpenReservation = 5,
    NestedReservable = 6,
    SharedChangeWithOpenRecords = 7,
    PreAllocatableRequiresRentable = 8,
    SharedRequiresRentable = 9,
    PreAllocatableAndSharedConflict = 10,
    DeactivateWithOpenRecords = 11,
    ActivateWithInactiveParent = 12,
    DeleteWithOpenRecords = 13,
    DeleteWithHistory = 14,
    DeleteWithChildren = 15
}

public enum SpaceChangeWarning
{
    ChildrenAreaExceedsParent = 1
}

public static class SpaceChangePolicy
{
    public static IReadOnlyList<SpaceChangeViolation> GetUpdateViolations(
        SpaceState current,
        SpaceState requested,
        SpaceOpenRecords openRecords,
        bool hasReservableAncestorOrDescendant)
    {
        var violations = new List<SpaceChangeViolation>();

        if (requested.ParentId != current.ParentId)
            violations.Add(SpaceChangeViolation.ParentChangeNotAllowed);

        if (requested.Area <= 0)
            violations.Add(SpaceChangeViolation.AreaNotPositive);
        else if (requested.Area != current.Area && openRecords.OnSelf.OpenLease)
            violations.Add(SpaceChangeViolation.AreaChangeWithOpenLease);

        if (requested.Usage != current.Usage)
            AddUsageViolations(current.Usage, requested.Usage, openRecords, hasReservableAncestorOrDescendant, violations);

        if (requested.IsShared != current.IsShared && openRecords.Any)
            violations.Add(SpaceChangeViolation.SharedChangeWithOpenRecords);

        AddFlagViolations(requested, violations);

        return violations;
    }

    public static IReadOnlyList<SpaceChangeViolation> GetDeactivateViolations(SpaceOpenRecords openRecords)
        => openRecords.Any ? [SpaceChangeViolation.DeactivateWithOpenRecords] : [];

    public static IReadOnlyList<SpaceChangeViolation> GetActivateViolations(bool hasParent, bool parentIsActive)
        => hasParent && !parentIsActive ? [SpaceChangeViolation.ActivateWithInactiveParent] : [];

    public static IReadOnlyList<SpaceChangeViolation> GetDeleteViolations(
        SpaceOpenRecords openRecords,
        bool hasHistory,
        bool hasChildren)
    {
        var violations = new List<SpaceChangeViolation>();

        if (openRecords.Any)
            violations.Add(SpaceChangeViolation.DeleteWithOpenRecords);
        if (hasHistory)
            violations.Add(SpaceChangeViolation.DeleteWithHistory);
        if (hasChildren)
            violations.Add(SpaceChangeViolation.DeleteWithChildren);

        return violations;
    }

    public static IReadOnlyList<SpaceChangeWarning> GetAreaWarnings(decimal parentArea, IEnumerable<decimal> childAreas)
        => childAreas.Sum() > parentArea ? [SpaceChangeWarning.ChildrenAreaExceedsParent] : [];

    private static void AddUsageViolations(
        SpaceUsage from,
        SpaceUsage to,
        SpaceOpenRecords openRecords,
        bool hasReservableAncestorOrDescendant,
        List<SpaceChangeViolation> violations)
    {
        switch (from)
        {
            case SpaceUsage.Rentable when openRecords.Any:
            case SpaceUsage.NotRentable when to == SpaceUsage.Rentable && openRecords.Any:
                violations.Add(SpaceChangeViolation.UsageChangeWithOpenRecords);
                break;
            case SpaceUsage.Reservable when openRecords.AnyOpenReservation:
                violations.Add(SpaceChangeViolation.UsageChangeWithOpenReservation);
                break;
        }

        if (to != SpaceUsage.Reservable)
            return;

        if (from != SpaceUsage.Rentable && openRecords.Any)
            violations.Add(SpaceChangeViolation.UsageChangeWithOpenRecords);

        if (hasReservableAncestorOrDescendant)
            violations.Add(SpaceChangeViolation.NestedReservable);
    }

    private static void AddFlagViolations(SpaceState state, List<SpaceChangeViolation> violations)
    {
        if (state.IsPreAllocatable && state.Usage != SpaceUsage.Rentable)
            violations.Add(SpaceChangeViolation.PreAllocatableRequiresRentable);
        if (state.IsShared && state.Usage != SpaceUsage.Rentable)
            violations.Add(SpaceChangeViolation.SharedRequiresRentable);
        if (state.IsPreAllocatable && state.IsShared)
            violations.Add(SpaceChangeViolation.PreAllocatableAndSharedConflict);
    }
}
