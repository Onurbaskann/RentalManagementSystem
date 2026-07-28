using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantFormViewModelValidator : IValidator<TenantFormViewModel>
{
    public ValidationResult Validate(TenantFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.TenantNo))
            errors.Add(new ValidationError("Kiracı No zorunludur.", nameof(input.TenantNo)));
        else if (input.TenantNo.Length > 20)
            errors.Add(new ValidationError("Kiracı No en fazla 20 karakter olabilir.", nameof(input.TenantNo)));

        if (!input.TenantCategoryId.HasValue || input.TenantCategoryId <= 0)
            errors.Add(new ValidationError("Kiracı kategorisi seçilmelidir.", nameof(input.TenantCategoryId)));

        if (!input.SectorId.HasValue || input.SectorId <= 0)
            errors.Add(new ValidationError("Sektör seçilmelidir.", nameof(input.SectorId)));

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Firma / Kurum Adı zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 200)
            errors.Add(new ValidationError("Firma / Kurum Adı en fazla 200 karakter olabilir.", nameof(input.Name)));

        if (string.IsNullOrWhiteSpace(input.TaxNo))
            errors.Add(new ValidationError("Vergi No zorunludur.", nameof(input.TaxNo)));
        else if (input.TaxNo.Length != 10 || !input.TaxNo.All(char.IsDigit))
            errors.Add(new ValidationError("Vergi No 10 haneli rakamdan oluşmalıdır.", nameof(input.TaxNo)));

        if (string.IsNullOrWhiteSpace(input.TaxOffice))
            errors.Add(new ValidationError("Vergi Dairesi zorunludur.", nameof(input.TaxOffice)));

        if (string.IsNullOrWhiteSpace(input.Phone))
            errors.Add(new ValidationError("Telefon zorunludur.", nameof(input.Phone)));
        else if (input.Phone.Length > 30)
            errors.Add(new ValidationError("Telefon en fazla 30 karakter olabilir.", nameof(input.Phone)));

        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new ValidationError("E-posta zorunludur.", nameof(input.Email)));
        else if (input.Email.Length > 200)
            errors.Add(new ValidationError("E-posta en fazla 200 karakter olabilir.", nameof(input.Email)));
        else if (!IsValidEmail(input.Email))
            errors.Add(new ValidationError("Geçerli bir e-posta adresi giriniz.", nameof(input.Email)));

        if (string.IsNullOrWhiteSpace(input.Address))
            errors.Add(new ValidationError("Adres zorunludur.", nameof(input.Address)));

        if (!string.IsNullOrWhiteSpace(input.InitialRepresentativeEmail))
        {
            if (input.InitialRepresentativeEmail.Length > 256)
                errors.Add(new ValidationError("İlk firma yetkilisi e-postası en fazla 256 karakter olabilir.", nameof(input.InitialRepresentativeEmail)));
            else if (!IsValidEmail(input.InitialRepresentativeEmail))
                errors.Add(new ValidationError("Geçerli bir e-posta adresi giriniz.", nameof(input.InitialRepresentativeEmail)));
        }

        if (input.InitialRepresentativeFullName?.Length > 200)
            errors.Add(new ValidationError("İlk firma yetkilisi adı en fazla 200 karakter olabilir.", nameof(input.InitialRepresentativeFullName)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }

    private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute EmailFormat = new();

    private static bool IsValidEmail(string email) => EmailFormat.IsValid(email);
}
