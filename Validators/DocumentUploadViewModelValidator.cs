using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class DocumentUploadViewModelValidator : IValidator<DocumentUploadViewModel>
{
    public ValidationResult Validate(DocumentUploadViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.OwnerType is not DocumentOwnerType.Tenant
            and not DocumentOwnerType.Lease
            and not DocumentOwnerType.Payment)
            errors.Add(new ValidationError("Geçersiz belge sahibi türü.", nameof(input.OwnerType)));

        if (input.OwnerId < 1)
            errors.Add(new ValidationError("Geçersiz belge sahibi.", nameof(input.OwnerId)));

        if (input.DocumentTypeId < 1)
            errors.Add(new ValidationError("Belge türü seçilmelidir.", nameof(input.DocumentTypeId)));

        if (input.File == null || input.File.Length == 0)
        {
            errors.Add(new ValidationError("Dosya seçilmedi.", nameof(input.File)));
        }
        else
        {
            if (Path.GetFileName(input.File.FileName).Length > 255)
                errors.Add(new ValidationError("Dosya adı en fazla 255 karakter olabilir.", nameof(input.File)));

            if (input.File.ContentType.Length > 100)
                errors.Add(new ValidationError("Dosya içerik türü en fazla 100 karakter olabilir.", nameof(input.File)));
        }

        if (input.Description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
