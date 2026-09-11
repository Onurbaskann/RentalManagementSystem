using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class LeaseLifecycleTests
{
    [Theory]
    [InlineData(LeaseStatus.Active, true)]
    [InlineData(LeaseStatus.Draft, false)]
    [InlineData(LeaseStatus.RevisionRequested, false)]
    [InlineData(LeaseStatus.Ended, false)]
    [InlineData(LeaseStatus.Terminated, false)]
    public void ActiveOperations_ShouldOnlyAllowActiveLease(
        LeaseStatus status,
        bool expected)
    {
        Assert.Equal(expected, LeaseLifecycle.CanExtend(status));
        Assert.Equal(expected, LeaseLifecycle.CanTerminate(status));
        Assert.Equal(expected, LeaseLifecycle.CanUpdateDueDate(status));
        Assert.Equal(expected, LeaseLifecycle.CanRegenerateCharges(status));
        Assert.Equal(expected, LeaseLifecycle.CanGenerateCharges(status));
    }
}
