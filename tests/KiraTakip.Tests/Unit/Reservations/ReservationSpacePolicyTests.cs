using KiraTakip.Domain.Reservations;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class ReservationSpacePolicyTests
{
    [Fact]
    public void ActiveReservable_IsReservable()
        => Assert.True(ReservationSpacePolicy.IsReservable(true, SpaceUsage.Reservable));

    [Fact]
    public void Inactive_IsNotReservable()
        => Assert.False(ReservationSpacePolicy.IsReservable(false, SpaceUsage.Reservable));

    [Theory]
    [InlineData(SpaceUsage.Rentable)]
    [InlineData(SpaceUsage.NotRentable)]
    public void NonReservableUsage_IsNotReservable(SpaceUsage usage)
        => Assert.False(ReservationSpacePolicy.IsReservable(true, usage));
}
