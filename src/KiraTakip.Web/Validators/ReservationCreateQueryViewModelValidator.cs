using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class ReservationCreateQueryViewModelValidator : IValidator<ReservationCreateQueryViewModel>
{
    public ValidationResult Validate(ReservationCreateQueryViewModel input)
        => input.SpaceId is null or > 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationError(
                "Alan bilgisi geçersizdir.",
                nameof(input.SpaceId)));
}
