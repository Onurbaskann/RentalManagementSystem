using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantChargeQueryViewModelValidator : IValidator<TenantChargeQueryViewModel>
{
    private static readonly HashSet<string> AllowedStatuses =
        ["tum", "bekliyor", "kismi", "tamodendi", "gecikti", "odeme_onay"];

    private static readonly HashSet<string> AllowedSources =
        ["lease", "manuel", "reservation"];

    public ValidationResult Validate(TenantChargeQueryViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.Page < 1)
            errors.Add(new ValidationError(
                "Sayfa numarası 1 veya daha büyük olmalıdır.",
                nameof(input.Page)));
        if (input.Size is < 1 or > 200)
            errors.Add(new ValidationError(
                "Sayfa boyutu 1 ile 200 arasında olmalıdır.",
                nameof(input.Size)));
        if (input.Q?.Length > 200)
            errors.Add(new ValidationError(
                "Arama metni en fazla 200 karakter olabilir.",
                nameof(input.Q)));
        if (input.UnitId <= 0)
            errors.Add(new ValidationError(
                "Geçerli bir birim seçilmelidir.",
                nameof(input.UnitId)));
        if (input.Year is < 2000 or > 2100)
            errors.Add(new ValidationError(
                "Yıl 2000 ile 2100 arasında olmalıdır.",
                nameof(input.Year)));
        if (!string.IsNullOrWhiteSpace(input.Status)
            && !AllowedStatuses.Contains(input.Status))
            errors.Add(new ValidationError(
                "Geçerli bir durum filtresi seçilmelidir.",
                nameof(input.Status)));
        if (!string.IsNullOrWhiteSpace(input.Source)
            && !AllowedSources.Contains(input.Source))
            errors.Add(new ValidationError(
                "Geçerli bir kaynak filtresi seçilmelidir.",
                nameof(input.Source)));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
