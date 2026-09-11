using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class LeaseApplicationLifecycleTests
{
    [Theory]
    [InlineData(LeaseStatus.Draft, LeaseStatus.RevisionRequested, true)]
    [InlineData(LeaseStatus.RevisionRequested, LeaseStatus.Draft, true)]
    [InlineData(LeaseStatus.Draft, LeaseStatus.Active, true)]
    [InlineData(LeaseStatus.Active, LeaseStatus.Draft, false)]
    [InlineData(LeaseStatus.RevisionRequested, LeaseStatus.Active, false)]
    public void CanTransition_ShouldFollowApplicationWorkflow(
        LeaseStatus from,
        LeaseStatus to,
        bool expected)
        => Assert.Equal(expected, LeaseApplicationLifecycle.CanTransition(from, to));

    [Theory]
    [InlineData(LeaseStatus.Draft, true, true)]
    [InlineData(LeaseStatus.RevisionRequested, false, true)]
    [InlineData(LeaseStatus.Active, false, false)]
    public void DraftOperations_ShouldFollowApplicationStatus(
        LeaseStatus status,
        bool canEdit,
        bool canDelete)
    {
        Assert.Equal(canEdit, LeaseApplicationLifecycle.CanEditDraft(status));
        Assert.Equal(canDelete, LeaseApplicationLifecycle.CanDeleteApplication(status));
    }
}
