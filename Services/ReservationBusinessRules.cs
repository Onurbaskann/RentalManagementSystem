using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces;
using System.Net.Mail;

namespace KiraTakip.Services;

public class ReservationBusinessRules(
    IReservationPolicyProvider policyProvider,
    TimeProvider timeProvider) : IReservationBusinessRules
{

    public DateTime GetCurrentTime()
        => TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            ResolveTimeZone(ReservationPolicySettings.TimeZoneId)).DateTime;

    public void EnsureScheduleIsValid(
        DateTime startDate,
        DateTime endDate,
        bool validatePastDate = true)
    {
        var policy = policyProvider.Current;
        Guard.InvalidField(
            endDate <= startDate,
            nameof(CreateReservationInput.EndDate),
            "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
            "RESERVATION_INVALID_DATE_RANGE");

        var currentTime = GetCurrentTime();
        Guard.InvalidField(
            validatePastDate && startDate < currentTime.AddMinutes(policy.MinimumAdvanceMinutes),
            nameof(CreateReservationInput.StartDate),
            policy.MinimumAdvanceMinutes == 0
                ? "Geçmiş bir tarih için rezervasyon oluşturulamaz."
                : $"Rezervasyon başlangıcı en az {policy.MinimumAdvanceMinutes} dakika sonrası olmalıdır.",
            "RESERVATION_START_DATE_IN_PAST");

        var durationMinutes = (endDate - startDate).TotalMinutes;
        Guard.InvalidField(
            durationMinutes < policy.MinimumDurationMinutes,
            nameof(CreateReservationInput.EndDate),
            $"Rezervasyon süresi en az {policy.MinimumDurationMinutes} dakika olmalıdır.",
            "RESERVATION_DURATION_TOO_SHORT");
        Guard.InvalidField(
            durationMinutes > policy.MaximumDurationMinutes,
            nameof(CreateReservationInput.EndDate),
            $"Rezervasyon süresi en fazla {policy.MaximumDurationMinutes} dakika olabilir.",
            "RESERVATION_DURATION_TOO_LONG");
        Guard.InvalidField(
            startDate > currentTime.AddDays(policy.MaximumAdvanceDays),
            nameof(CreateReservationInput.StartDate),
            $"En fazla {policy.MaximumAdvanceDays} gün sonrası için rezervasyon oluşturulabilir.",
            "RESERVATION_TOO_FAR_IN_ADVANCE");
    }

    public void EnsureTransitionAllowed(
        ReservationStatus currentStatus,
        ReservationStatus targetStatus)
        => Guard.Conflict(
            !ReservationLifecycle.CanTransition(currentStatus, targetStatus),
            "Bu rezervasyon için istenen durum değişikliği yapılamaz.",
            "RESERVATION_STATUS_TRANSITION_NOT_ALLOWED");

    public void EnsureContentIsValid(ReservationContentPolicyInput input)
    {
        var policy = policyProvider.Current;
        Guard.InvalidField(
            string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 200,
            nameof(input.Title),
            "Toplantı başlığı zorunlu ve en fazla 200 karakter olmalıdır.",
            "RESERVATION_INVALID_TITLE");
        Guard.InvalidField(
            input.Description?.Length > 500,
            nameof(input.Description),
            "Açıklama en fazla 500 karakter olabilir.",
            "RESERVATION_DESCRIPTION_TOO_LONG");
        Guard.InvalidField(
            input.Notes?.Length > 2000,
            nameof(input.Notes),
            "Toplantı notları en fazla 2000 karakter olabilir.",
            "RESERVATION_NOTES_TOO_LONG");
        Guard.InvalidField(
            input.InternalNotes?.Length > 2000,
            nameof(input.InternalNotes),
            "İç notlar en fazla 2000 karakter olabilir.",
            "RESERVATION_INTERNAL_NOTES_TOO_LONG");
        Guard.InvalidField(
            input.Attendees.Count > policy.MaximumAttendeeCount,
            nameof(input.Attendees),
            $"En fazla {policy.MaximumAttendeeCount} katılımcı eklenebilir.",
            "RESERVATION_ATTENDEE_LIMIT_EXCEEDED");
        Guard.InvalidField(
            input.Attendees.Count(attendee => attendee.IsReservationOwner) != 1,
            nameof(input.Attendees),
            "Katılımcı listesinde bir rezervasyon sahibi bulunmalıdır.",
            "RESERVATION_OWNER_REQUIRED");

        var normalizedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attendee in input.Attendees)
        {
            Guard.InvalidField(
                string.IsNullOrWhiteSpace(attendee.DisplayName)
                || attendee.DisplayName.Trim().Length > 200,
                nameof(input.Attendees),
                "Katılımcı adı zorunlu ve en fazla 200 karakter olmalıdır.",
                "RESERVATION_INVALID_ATTENDEE_NAME");
            Guard.InvalidField(
                string.IsNullOrWhiteSpace(attendee.EmailAddress)
                || attendee.EmailAddress.Length > 256
                || !MailAddress.TryCreate(attendee.EmailAddress, out _),
                nameof(input.Attendees),
                "Geçerli bir katılımcı e-posta adresi giriniz.",
                "RESERVATION_INVALID_ATTENDEE_EMAIL");

            Guard.InvalidField(
                !normalizedEmails.Add(attendee.EmailAddress!.Trim()),
                nameof(input.Attendees),
                "Aynı e-posta adresi katılımcı listesine birden fazla eklenemez.",
                "RESERVATION_DUPLICATE_ATTENDEE_EMAIL");
        }
    }

    public void EnsureUnitIsReservable(
        ReservationUnitContextDto unit,
        bool fieldValidation = false)
    {
        var invalid = !unit.IsUnitActive
            || !unit.IsUnitTypeActive
            || unit.Usage != UnitTypeUsage.Reservable;

        if (fieldValidation)
        {
            Guard.InvalidField(
                invalid,
                nameof(unit.UnitId),
                "Seçilen birim aktif veya rezervasyona uygun değildir.",
                "RESERVATION_UNIT_NOT_RESERVABLE");
            return;
        }

        Guard.Against(
            invalid,
            "Seçilen birim aktif veya rezervasyona uygun değildir.",
            "RESERVATION_UNIT_NOT_RESERVABLE");
    }

    public void EnsureAccessScope(
        int propertyId,
        int unitId,
        ReservationAccessScopeInput accessScope)
    {
        if (accessScope.PropertyIds == null && accessScope.UnitIds == null)
            return;

        var hasPropertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var hasUnitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !hasPropertyAccess && !hasUnitAccess,
            "Bu rezervasyon yetki kapsamınızın dışındadır.",
            "RESERVATION_OUT_OF_SCOPE");
    }

    public void EnsureCancellationAllowed(
        Reservation reservation,
        bool canOverrideTimeRestriction)
    {
        var policy = policyProvider.Current;
        Guard.Conflict(
            reservation.Status == ReservationStatus.Cancelled,
            "Bu rezervasyon zaten iptal edilmiş.",
            "RESERVATION_ALREADY_CANCELLED");
        Guard.Conflict(
            !ReservationLifecycle.CanTransition(reservation.Status, ReservationStatus.Cancelled),
            "Bu durumdaki rezervasyon iptal edilemez.",
            "RESERVATION_CANNOT_BE_CANCELLED");
        Guard.Conflict(
            !canOverrideTimeRestriction
            && reservation.StartDate <= GetCurrentTime().AddMinutes(policy.ModificationCutoffMinutes),
            $"Rezervasyon başlangıcına {policy.ModificationCutoffMinutes} dakikadan az kaldığı için iptal edilemez.",
            "RESERVATION_CANCELLATION_CUTOFF_EXCEEDED");
    }

    public void EnsureModificationAllowed(
        Reservation reservation,
        bool canOverrideTimeRestriction,
        string? overrideReason)
    {
        var policy = policyProvider.Current;
        Guard.Conflict(
            reservation.Status is not ReservationStatus.PendingApproval
                and not ReservationStatus.Confirmed,
            "Bu durumdaki rezervasyon güncellenemez.",
            "RESERVATION_CANNOT_BE_MODIFIED");

        var cutoffExceeded = reservation.StartDate
            <= GetCurrentTime().AddMinutes(policy.ModificationCutoffMinutes);
        Guard.Conflict(
            cutoffExceeded && !canOverrideTimeRestriction,
            $"Rezervasyon başlangıcına {policy.ModificationCutoffMinutes} dakikadan az kaldığı için güncellenemez.",
            "RESERVATION_MODIFICATION_CUTOFF_EXCEEDED");
        Guard.Against(
            cutoffExceeded
                && canOverrideTimeRestriction
                && (string.IsNullOrWhiteSpace(overrideReason) || overrideReason.Trim().Length > 450),
            "Zaman sınırı istisnası için en fazla 450 karakterlik bir gerekçe girilmelidir.",
            "RESERVATION_OVERRIDE_REASON_REQUIRED");
    }

    public void EnsureTenantOwnership(int reservationTenantId, int currentTenantId)
        => Guard.Forbidden(
            reservationTenantId != currentTenantId,
            "Bu rezervasyon başka bir kiracıya aittir.",
            "RESERVATION_TENANT_SCOPE_VIOLATION");

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new InvalidOperationException("Rezervasyon saat dilimi ayarı boş olamaz.");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Rezervasyon saat dilimi bulunamadı: {timeZoneId}",
                exception);
        }
    }
}
