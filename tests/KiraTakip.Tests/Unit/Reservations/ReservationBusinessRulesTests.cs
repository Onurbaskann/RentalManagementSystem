using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

public class ReservationBusinessRulesTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 13, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CurrentTime_ShouldUseConfiguredTurkeyTimeZone()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);

        Assert.Equal(new DateTime(2026, 8, 13, 10, 0, 0), rules.GetCurrentTime());
    }

    [Theory]
    [InlineData(-1, 60, "RESERVATION_START_DATE_IN_PAST")]
    [InlineData(60, 74, "RESERVATION_DURATION_TOO_SHORT")]
    [InlineData(60, 1501, "RESERVATION_DURATION_TOO_LONG")]
    [InlineData(527101, 527161, "RESERVATION_TOO_FAR_IN_ADVANCE")]
    public void Schedule_ShouldRejectPolicyViolations(
        int startOffsetMinutes,
        int endOffsetMinutes,
        string expectedCode)
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);
        var currentTime = rules.GetCurrentTime();

        var exception = Assert.Throws<BusinessValidationException>(() =>
            rules.EnsureScheduleIsValid(
                currentTime.AddMinutes(startOffsetMinutes),
                currentTime.AddMinutes(endOffsetMinutes)));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void CancellationCutoff_ShouldRequireExplicitOverride()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);
        var reservation = new Reservation
        {
            Status = ReservationStatus.Confirmed,
            StartDate = rules.GetCurrentTime().AddMinutes(119)
        };

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureCancellationAllowed(reservation, canOverrideTimeRestriction: false));
        Assert.Equal("RESERVATION_CANCELLATION_CUTOFF_EXCEEDED", exception.Code);

        rules.EnsureCancellationAllowed(reservation, canOverrideTimeRestriction: true);
    }

    [Fact]
    public void ModificationCutoff_ShouldRequirePermissionAndReason()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);
        var reservation = new Reservation
        {
            Status = ReservationStatus.Confirmed,
            StartDate = rules.GetCurrentTime().AddMinutes(119)
        };

        var noPermission = Assert.Throws<BusinessException>(() =>
            rules.EnsureModificationAllowed(reservation, false, null));
        Assert.Equal("RESERVATION_MODIFICATION_CUTOFF_EXCEEDED", noPermission.Code);

        var noReason = Assert.Throws<BusinessException>(() =>
            rules.EnsureModificationAllowed(reservation, true, null));
        Assert.Equal("RESERVATION_OVERRIDE_REASON_REQUIRED", noReason.Code);

        rules.EnsureModificationAllowed(reservation, true, "Yönetim kararı");
    }

    [Fact]
    public void AccessScope_ShouldCombinePropertyAndDirectUnitAccess()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);

        rules.EnsureAccessScope(10, 20, new ReservationAccessScopeInput([10], []));
        rules.EnsureAccessScope(10, 20, new ReservationAccessScopeInput([], [20]));

        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureAccessScope(10, 20, new ReservationAccessScopeInput([], [])));
        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
    }

    [Fact]
    public void TenantOwnership_ShouldRejectAnotherTenant()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);

        rules.EnsureTenantOwnership(10, 10);
        var exception = Assert.Throws<BusinessException>(() =>
            rules.EnsureTenantOwnership(10, 11));

        Assert.Equal("RESERVATION_TENANT_SCOPE_VIOLATION", exception.Code);
        Assert.Equal(ErrorType.Forbidden, exception.ErrorType);
    }

    [Fact]
    public void Content_ShouldRejectDuplicateAttendeeEmail()
    {
        var rules = ReservationBusinessRulesTestFactory.Create(UtcNow);
        var input = new ReservationContentPolicyInput(
            "Planlama Toplantısı",
            null,
            null,
            null,
            [
                new ReservationAttendeePolicyInput(
                    "Rezervasyon Sahibi", "owner@example.com", true),
                new ReservationAttendeePolicyInput(
                    "Tekrar Eden", "OWNER@example.com", false)
            ]);

        var exception = Assert.Throws<BusinessValidationException>(() =>
            rules.EnsureContentIsValid(input));

        Assert.Equal("RESERVATION_DUPLICATE_ATTENDEE_EMAIL", exception.Code);
    }
}
