using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class BankTransactionMatchViewModelValidator : IValidator<BankTransactionMatchViewModel>
{
    public ValidationResult Validate(BankTransactionMatchViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.PaymentId < 1)
            errors.Add(new ValidationError("Ödeme seçilmelidir.", nameof(input.PaymentId)));

        if (input.BankTransactionId < 1)
            errors.Add(new ValidationError("Banka hareketi seçilmelidir.", nameof(input.BankTransactionId)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
