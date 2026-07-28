using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class BankTransactionImportViewModelValidator : IValidator<BankTransactionImportViewModel>
{
    public ValidationResult Validate(BankTransactionImportViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.BankCode))
            errors.Add(new ValidationError("Banka seçilmelidir.", nameof(input.BankCode)));

        if (input.File == null || input.File.Length == 0)
            errors.Add(new ValidationError("CSV dosyası seçiniz.", nameof(input.File)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
