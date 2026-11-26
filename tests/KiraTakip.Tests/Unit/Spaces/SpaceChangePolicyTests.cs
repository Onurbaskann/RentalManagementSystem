using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class SpaceChangePolicyTests
{
    private static readonly SpaceState Rentable = new(1, SpaceUsage.Rentable, 100m, false, false);
    private static readonly SpaceState Reservable = Rentable with { Usage = SpaceUsage.Reservable };
    private static readonly SpaceState NotRentable = Rentable with { Usage = SpaceUsage.NotRentable };

    private static readonly OpenRecordFlags Lease = new(true, false, false);
    private static readonly OpenRecordFlags PreAllocation = new(false, true, false);
    private static readonly OpenRecordFlags Reservation = new(false, false, true);

    private static SpaceOpenRecords OnSelf(OpenRecordFlags flags)
        => new(flags, OpenRecordFlags.None, OpenRecordFlags.None);

    private static SpaceOpenRecords OnSubtree(OpenRecordFlags flags)
        => new(OpenRecordFlags.None, OpenRecordFlags.None, flags);

    private static SpaceOpenRecords OnAncestors(OpenRecordFlags flags)
        => new(OpenRecordFlags.None, flags, OpenRecordFlags.None);

    private static IReadOnlyList<SpaceChangeViolation> Update(
        SpaceState current, SpaceState requested, SpaceOpenRecords open, bool reservableRelative = false)
        => SpaceChangePolicy.GetUpdateViolations(current, requested, open, reservableRelative);

    [Fact]
    public void Update_ShouldBeEmpty_WhenNothingChanges()
        => Assert.Empty(Update(Rentable, Rentable, OnSelf(Lease)));

    [Fact]
    public void Update_ShouldAllowAreaChange_WhenNoOpenLease()
    {
        var requested = Rentable with { Area = 80m };

        Assert.Empty(Update(Rentable, requested, SpaceOpenRecords.None));
        Assert.Empty(Update(Rentable, requested, OnSelf(PreAllocation)));
        Assert.Empty(Update(Rentable, requested, OnSubtree(Lease)));
        Assert.Empty(Update(Rentable, requested, OnAncestors(Lease)));
    }

    [Fact]
    public void Update_ShouldRejectAreaChange_WhenOpenLeaseOnSelf()
        => Assert.Equal([SpaceChangeViolation.AreaChangeWithOpenLease],
            Update(Rentable, Rentable with { Area = 80m }, OnSelf(Lease)));

    [Fact]
    public void Update_ShouldRejectNonPositiveArea()
        => Assert.Equal([SpaceChangeViolation.AreaNotPositive],
            Update(Rentable, Rentable with { Area = 0m }, SpaceOpenRecords.None));

    [Fact]
    public void Update_ShouldAlwaysRejectParentChange()
    {
        var requested = Rentable with { ParentId = 2 };

        Assert.Equal([SpaceChangeViolation.ParentChangeNotAllowed], Update(Rentable, requested, SpaceOpenRecords.None));
        Assert.Equal([SpaceChangeViolation.ParentChangeNotAllowed], Update(Rentable, requested, OnSelf(Lease)));
    }

    [Theory]
    [InlineData(SpaceUsage.Reservable)]
    [InlineData(SpaceUsage.NotRentable)]
    public void Update_ShouldAllowLeavingRentable_WhenNoOpenRecords(SpaceUsage target)
        => Assert.Empty(Update(Rentable, Rentable with { Usage = target }, SpaceOpenRecords.None));

    [Fact]
    public void Update_ShouldRejectLeavingRentable_ForEachOpenRecordScope()
    {
        var requested = NotRentable;

        Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords], Update(Rentable, requested, OnSelf(Lease)));
        Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords], Update(Rentable, requested, OnAncestors(PreAllocation)));
        Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords], Update(Rentable, requested, OnSubtree(Reservation)));
    }

    [Fact]
    public void Update_ShouldAllowNotRentableToRentable_WhenNoOpenRecords()
        => Assert.Empty(Update(NotRentable, Rentable, SpaceOpenRecords.None));

    [Fact]
    public void Update_ShouldRejectNotRentableToRentable_WhenOpenRecords()
        => Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords],
            Update(NotRentable, Rentable, OnSubtree(Lease)));

    [Fact]
    public void Update_ShouldAllowNotRentableToReservable_WhenNoOpenRecords()
        => Assert.Empty(Update(NotRentable, Reservable, SpaceOpenRecords.None));

    [Fact]
    public void Update_ShouldAllowLeavingReservable_WhenOnlyNonReservationRecordsOpen()
    {
        Assert.Empty(Update(Reservable, Rentable, SpaceOpenRecords.None));
        Assert.Empty(Update(Reservable, Rentable, OnSelf(Lease)));
    }

    [Fact]
    public void Update_ShouldRejectLeavingReservable_WhenOpenReservation()
        => Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenReservation],
            Update(Reservable, Rentable, OnSelf(Reservation)));

    [Fact]
    public void Update_ShouldRejectToReservable_WhenOpenRecords()
    {
        Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords], Update(Rentable, Reservable, OnSelf(Lease)));
        Assert.Equal([SpaceChangeViolation.UsageChangeWithOpenRecords], Update(NotRentable, Reservable, OnAncestors(Lease)));
    }

    [Fact]
    public void Update_ShouldRejectToReservable_WhenAncestorOrDescendantReservable()
        => Assert.Equal([SpaceChangeViolation.NestedReservable],
            Update(Rentable, Reservable, SpaceOpenRecords.None, reservableRelative: true));

    [Fact]
    public void Update_ShouldIgnoreReservableRelative_WhenNotMovingToReservable()
        => Assert.Empty(Update(Rentable, NotRentable, SpaceOpenRecords.None, reservableRelative: true));

    [Fact]
    public void Update_ShouldAllowPreAllocatableToggle_EvenWithOpenRecords()
    {
        var on = Rentable with { IsPreAllocatable = true };

        Assert.Empty(Update(Rentable, on, OnSelf(Lease)));
        Assert.Empty(Update(on, Rentable, OnSelf(PreAllocation)));
    }

    [Fact]
    public void Update_ShouldAllowSharedToggle_WhenNoOpenRecords()
    {
        var shared = Rentable with { IsShared = true };

        Assert.Empty(Update(Rentable, shared, SpaceOpenRecords.None));
        Assert.Empty(Update(shared, Rentable, SpaceOpenRecords.None));
    }

    [Fact]
    public void Update_ShouldRejectSharedToggle_WhenOpenRecords()
    {
        var shared = Rentable with { IsShared = true };

        Assert.Equal([SpaceChangeViolation.SharedChangeWithOpenRecords], Update(Rentable, shared, OnSelf(Lease)));
        Assert.Equal([SpaceChangeViolation.SharedChangeWithOpenRecords], Update(shared, Rentable, OnSubtree(Lease)));
    }

    [Fact]
    public void Update_ShouldRejectFlags_WhenNotRentable()
    {
        var requested = NotRentable with { IsPreAllocatable = true, IsShared = true };

        var violations = Update(NotRentable, requested, SpaceOpenRecords.None);

        Assert.Contains(SpaceChangeViolation.PreAllocatableRequiresRentable, violations);
        Assert.Contains(SpaceChangeViolation.SharedRequiresRentable, violations);
        Assert.Contains(SpaceChangeViolation.PreAllocatableAndSharedConflict, violations);
    }

    [Fact]
    public void Update_ShouldRejectPreAllocatableAndSharedTogether()
        => Assert.Equal([SpaceChangeViolation.PreAllocatableAndSharedConflict],
            Update(Rentable, Rentable with { IsPreAllocatable = true, IsShared = true }, SpaceOpenRecords.None));

    [Fact]
    public void Deactivate_ShouldRespectOpenRecords()
    {
        Assert.Empty(SpaceChangePolicy.GetDeactivateViolations(SpaceOpenRecords.None));
        Assert.Equal([SpaceChangeViolation.DeactivateWithOpenRecords],
            SpaceChangePolicy.GetDeactivateViolations(OnSubtree(Lease)));
        Assert.Equal([SpaceChangeViolation.DeactivateWithOpenRecords],
            SpaceChangePolicy.GetDeactivateViolations(OnSelf(Reservation)));
    }

    [Fact]
    public void Activate_ShouldRequireActiveParent_ButAllowRoot()
    {
        Assert.Empty(SpaceChangePolicy.GetActivateViolations(hasParent: false, parentIsActive: false));
        Assert.Empty(SpaceChangePolicy.GetActivateViolations(hasParent: true, parentIsActive: true));
        Assert.Equal([SpaceChangeViolation.ActivateWithInactiveParent],
            SpaceChangePolicy.GetActivateViolations(hasParent: true, parentIsActive: false));
    }

    [Fact]
    public void Delete_ShouldBeEmpty_WhenNoRecordsHistoryOrChildren()
        => Assert.Empty(SpaceChangePolicy.GetDeleteViolations(SpaceOpenRecords.None, false, false));

    [Fact]
    public void Delete_ShouldReportEachBlocker()
    {
        Assert.Equal([SpaceChangeViolation.DeleteWithOpenRecords],
            SpaceChangePolicy.GetDeleteViolations(OnSelf(Lease), false, false));
        Assert.Equal([SpaceChangeViolation.DeleteWithHistory],
            SpaceChangePolicy.GetDeleteViolations(SpaceOpenRecords.None, true, false));
        Assert.Equal([SpaceChangeViolation.DeleteWithChildren],
            SpaceChangePolicy.GetDeleteViolations(SpaceOpenRecords.None, false, true));
        Assert.Equal(3, SpaceChangePolicy.GetDeleteViolations(OnSelf(Lease), true, true).Count);
    }

    [Fact]
    public void AreaWarning_ShouldWarn_WhenChildrenExceedParent()
        => Assert.Equal([SpaceChangeWarning.ChildrenAreaExceedsParent],
            SpaceChangePolicy.GetAreaWarnings(100m, [60m, 60m]));

    [Fact]
    public void AreaWarning_ShouldNotWarn_WhenChildrenFitParent()
    {
        Assert.Empty(SpaceChangePolicy.GetAreaWarnings(120m, [60m, 40m]));
        Assert.Empty(SpaceChangePolicy.GetAreaWarnings(100m, [60m, 40m]));
        Assert.Empty(SpaceChangePolicy.GetAreaWarnings(100m, []));
    }
}
