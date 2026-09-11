using KiraTakip.Domain.Reservations;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class ReservationLifecycleTests
{
    [Fact]
    public void CanComplete_WhenConfirmed_ReturnsTrue()
    {
        var result = ReservationLifecycle.CanComplete(ReservationStatus.Confirmed);

        Assert.True(result);
    }

    [Theory]
    [InlineData(ReservationStatus.PendingApproval)]
    [InlineData(ReservationStatus.Completed)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Rejected)]
    public void CanComplete_WhenNotConfirmed_ReturnsFalse(ReservationStatus status)
    {
        var result = ReservationLifecycle.CanComplete(status);

        Assert.False(result);
    }

    [Fact]
    public void CanTransferToCharge_WhenConfirmed_ReturnsTrue()
    {
        var result = ReservationLifecycle.CanTransferToCharge(ReservationStatus.Confirmed);

        Assert.True(result);
    }

    [Theory]
    [InlineData(ReservationStatus.PendingApproval)]
    [InlineData(ReservationStatus.Completed)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Rejected)]
    public void CanTransferToCharge_WhenNotConfirmed_ReturnsFalse(ReservationStatus status)
    {
        var result = ReservationLifecycle.CanTransferToCharge(status);

        Assert.False(result);
    }
}
