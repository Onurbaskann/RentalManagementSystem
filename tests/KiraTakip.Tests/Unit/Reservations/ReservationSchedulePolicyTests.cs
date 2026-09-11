using KiraTakip.Domain.Reservations;
using Xunit;

namespace KiraTakip.Tests;

public class ReservationSchedulePolicyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, -15)]
    [InlineData(60, -60)]
    public void CalculateCompletionCutoff_SubtractsGraceMinutesFromCurrentTime(int graceMinutes, int expectedMinutesOffset)
    {
        var currentTime = new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc);
        var expectedCutoff = currentTime.AddMinutes(expectedMinutesOffset);

        var actualCutoff = ReservationSchedulePolicy.CalculateCompletionCutoff(currentTime, graceMinutes);

        Assert.Equal(expectedCutoff, actualCutoff);
    }

    [Fact]
    public void HasReachedCompletionCutoff_WhenEndDateIsBeforeCutoff_ReturnsTrue()
    {
        var cutoff = new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc);
        var endDate = cutoff.AddMinutes(-5);

        var result = ReservationSchedulePolicy.HasReachedCompletionCutoff(endDate, cutoff);

        Assert.True(result);
    }

    [Fact]
    public void HasReachedCompletionCutoff_WhenEndDateEqualsCutoff_ReturnsTrue()
    {
        var cutoff = new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc);
        var endDate = cutoff;

        var result = ReservationSchedulePolicy.HasReachedCompletionCutoff(endDate, cutoff);

        Assert.True(result);
    }

    [Fact]
    public void HasReachedCompletionCutoff_WhenEndDateIsAfterCutoff_ReturnsFalse()
    {
        var cutoff = new DateTime(2026, 9, 8, 14, 0, 0, DateTimeKind.Utc);
        var endDate = cutoff.AddSeconds(1);

        var result = ReservationSchedulePolicy.HasReachedCompletionCutoff(endDate, cutoff);

        Assert.False(result);
    }
}
