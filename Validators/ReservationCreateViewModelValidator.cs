using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;
using System.Net.Mail;

namespace KiraTakip.Validators;

public class ReservationCreateViewModelValidator : IValidator<ReservationCreateViewModel>
{
    public ValidationResult Validate(ReservationCreateViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.TenantId is null or <= 0)
            errors.Add(new ValidationError("Kiracı seçilmelidir.", nameof(input.TenantId)));

        ValidateCommon(
            input.UnitId,
            input.StartDate,
            input.EndDate,
            input.Title,
            input.Description,
            input.Notes,
            input.Attendees,
            errors);

        if (input.InternalNotes?.Length > 2000)
            errors.Add(new ValidationError("İç notlar en fazla 2000 karakter olabilir.", nameof(input.InternalNotes)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }

    internal static void ValidateCommon(
        int? unitId,
        DateTime startDate,
        DateTime endDate,
        string? title,
        string? description,
        string? notes,
        IReadOnlyList<ReservationAttendeeInputViewModel> attendees,
        List<ValidationError> errors)
    {
        if (unitId is null or <= 0)
            errors.Add(new ValidationError("Taşınmaz birimi seçilmelidir.", nameof(ReservationCreateViewModel.UnitId)));

        if (startDate == default)
            errors.Add(new ValidationError("Başlangıç tarihi zorunludur.", nameof(ReservationCreateViewModel.StartDate)));

        if (endDate == default)
            errors.Add(new ValidationError("Bitiş tarihi zorunludur.", nameof(ReservationCreateViewModel.EndDate)));
        else if (startDate != default && endDate <= startDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıçtan sonra olmalıdır.", nameof(ReservationCreateViewModel.EndDate)));

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            errors.Add(new ValidationError("Toplantı başlığı zorunlu ve en fazla 200 karakter olmalıdır.", nameof(ReservationCreateViewModel.Title)));
        if (description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(ReservationCreateViewModel.Description)));
        if (notes?.Length > 2000)
            errors.Add(new ValidationError("Notlar en fazla 2000 karakter olabilir.", nameof(ReservationCreateViewModel.Notes)));

        var normalizedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < attendees.Count; index++)
        {
            var attendee = attendees[index];
            var hasName = !string.IsNullOrWhiteSpace(attendee.DisplayName);
            var hasEmail = !string.IsNullOrWhiteSpace(attendee.EmailAddress);
            if (!hasName && !hasEmail) continue;

            if (!hasName || attendee.DisplayName!.Trim().Length > 200)
                errors.Add(new ValidationError("Katılımcı adı zorunlu ve en fazla 200 karakter olmalıdır.", $"Attendees[{index}].DisplayName"));
            if (!hasEmail || attendee.EmailAddress!.Length > 320 || !IsValidEmail(attendee.EmailAddress))
                errors.Add(new ValidationError("Geçerli bir katılımcı e-posta adresi girilmelidir.", $"Attendees[{index}].EmailAddress"));
            else if (!normalizedEmails.Add(attendee.EmailAddress.Trim()))
                errors.Add(new ValidationError("Aynı e-posta adresi birden fazla katılımcı için kullanılamaz.", $"Attendees[{index}].EmailAddress"));
        }
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email).Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
}

public class TenantReservationCreateViewModelValidator : IValidator<TenantReservationCreateViewModel>
{
    public ValidationResult Validate(TenantReservationCreateViewModel input)
    {
        var errors = new List<ValidationError>();
        ReservationCreateViewModelValidator.ValidateCommon(
            input.UnitId,
            input.StartDate,
            input.EndDate,
            input.Title,
            input.Description,
            input.Notes,
            input.Attendees,
            errors);
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public class ReservationEditViewModelValidator : IValidator<ReservationEditViewModel>
{
    public ValidationResult Validate(ReservationEditViewModel input)
    {
        var errors = new List<ValidationError>();
        if (input.Id <= 0)
            errors.Add(new ValidationError("Rezervasyon bilgisi geçersizdir.", nameof(input.Id)));
        if (input.TenantId is null or <= 0)
            errors.Add(new ValidationError("Kiracı seçilmelidir.", nameof(input.TenantId)));
        if (input.RowVersion.Length == 0)
            errors.Add(new ValidationError("Rezervasyon sürüm bilgisi eksik. Sayfayı yenileyip tekrar deneyin.", nameof(input.RowVersion)));
        ReservationCreateViewModelValidator.ValidateCommon(
            input.UnitId,
            input.StartDate,
            input.EndDate,
            input.Title,
            input.Description,
            input.Notes,
            input.Attendees,
            errors);
        if (input.InternalNotes?.Length > 2000)
            errors.Add(new ValidationError("İç notlar en fazla 2000 karakter olabilir.", nameof(input.InternalNotes)));
        if (input.OverrideReason?.Length > 450)
            errors.Add(new ValidationError("İstisna gerekçesi en fazla 450 karakter olabilir.", nameof(input.OverrideReason)));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
