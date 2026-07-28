using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class DocumentTypeFormViewModelValidator : IValidator<DocumentTypeFormViewModel>
{
    public ValidationResult Validate(DocumentTypeFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Ad zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 200)
            errors.Add(new ValidationError("Ad en fazla 200 karakter olabilir.", nameof(input.Name)));

        if (input.Description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        if (string.IsNullOrWhiteSpace(input.AllowedExtensions))
            errors.Add(new ValidationError("İzin verilen uzantılar zorunludur.", nameof(input.AllowedExtensions)));
        else if (input.AllowedExtensions.Length > 200)
            errors.Add(new ValidationError("İzin verilen uzantılar en fazla 200 karakter olabilir.", nameof(input.AllowedExtensions)));

        if (input.MaxSizeMb is < 1 or > 100)
            errors.Add(new ValidationError("Maksimum boyut 1-100 MB arasında olmalıdır.", nameof(input.MaxSizeMb)));

        if (input.SortOrder is < 1 or > 9999)
            errors.Add(new ValidationError("Sıra 1-9999 arasında olmalıdır.", nameof(input.SortOrder)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
